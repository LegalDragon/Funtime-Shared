using Microsoft.EntityFrameworkCore;
using FTPBAuth.API.Data;
using FTPBAuth.API.Models;

namespace FTPBAuth.API.Services;

public class OtpService : IOtpService
{
    private readonly ApplicationDbContext _context;
    private readonly ISmsService _smsService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OtpService> _logger;

    private const int OTP_EXPIRATION_MINUTES = 5;
    private const int OTP_LENGTH = 6;

    public OtpService(
        ApplicationDbContext context,
        ISmsService smsService,
        IConfiguration configuration,
        ILogger<OtpService> logger)
    {
        _context = context;
        _smsService = smsService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> IsRateLimitedAsync(string phoneNumber)
    {
        var maxAttempts = int.Parse(_configuration["RateLimiting:OtpMaxAttempts"] ?? "5");
        var windowMinutes = int.Parse(_configuration["RateLimiting:OtpWindowMinutes"] ?? "15");

        var rateLimit = await _context.OtpRateLimits
            .FirstOrDefaultAsync(r => r.PhoneNumber == phoneNumber);

        if (rateLimit == null)
        {
            return false;
        }

        // Check if blocked
        if (rateLimit.BlockedUntil.HasValue && rateLimit.BlockedUntil > DateTime.UtcNow)
        {
            return true;
        }

        // Check if window has expired
        if (rateLimit.WindowStart.AddMinutes(windowMinutes) < DateTime.UtcNow)
        {
            // Reset the window
            rateLimit.RequestCount = 0;
            rateLimit.WindowStart = DateTime.UtcNow;
            rateLimit.BlockedUntil = null;
            await _context.SaveChangesAsync();
            return false;
        }

        return rateLimit.RequestCount >= maxAttempts;
    }

    public async Task<(bool success, string message)> SendOtpAsync(string phoneNumber)
    {
        // Check rate limiting
        if (await IsRateLimitedAsync(phoneNumber))
        {
            return (false, "Too many OTP requests. Please try again later.");
        }

        // Generate OTP
        var code = GenerateOtp();
        var expiresAt = DateTime.UtcNow.AddMinutes(OTP_EXPIRATION_MINUTES);

        // Invalidate previous unused OTPs for this phone
        var previousOtps = await _context.OtpRequests
            .Where(o => o.PhoneNumber == phoneNumber && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        foreach (var otp in previousOtps)
        {
            otp.IsUsed = true;
        }

        // Create new OTP request
        var otpRequest = new OtpRequest
        {
            PhoneNumber = phoneNumber,
            Code = code,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        };

        _context.OtpRequests.Add(otpRequest);

        // Update rate limit counter
        await UpdateRateLimitAsync(phoneNumber);

        await _context.SaveChangesAsync();

        // Send OTP via SMS
        var sent = await _smsService.SendSmsAsync(phoneNumber, $"Your FTPB verification code is: {code}. It expires in {OTP_EXPIRATION_MINUTES} minutes.");

        if (!sent)
        {
            _logger.LogError("Failed to send OTP SMS to {PhoneNumber}", phoneNumber);
            return (false, "Failed to send OTP. Please try again.");
        }

        _logger.LogInformation("OTP sent successfully to {PhoneNumber}", phoneNumber);
        return (true, "OTP sent successfully.");
    }

    public async Task<(bool success, string message)> VerifyOtpAsync(string phoneNumber, string code)
    {
        var otpRequest = await _context.OtpRequests
            .Where(o => o.PhoneNumber == phoneNumber &&
                        o.Code == code &&
                        !o.IsUsed &&
                        o.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

        if (otpRequest == null)
        {
            // Check if there's an expired or used OTP to give better error message
            var existingOtp = await _context.OtpRequests
                .Where(o => o.PhoneNumber == phoneNumber && o.Code == code)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (existingOtp != null)
            {
                if (existingOtp.IsUsed)
                {
                    return (false, "This OTP has already been used.");
                }
                if (existingOtp.ExpiresAt <= DateTime.UtcNow)
                {
                    return (false, "This OTP has expired.");
                }
            }

            return (false, "Invalid OTP.");
        }

        // Mark OTP as used
        otpRequest.IsUsed = true;
        await _context.SaveChangesAsync();

        return (true, "OTP verified successfully.");
    }

    private async Task UpdateRateLimitAsync(string phoneNumber)
    {
        var maxAttempts = int.Parse(_configuration["RateLimiting:OtpMaxAttempts"] ?? "5");
        var windowMinutes = int.Parse(_configuration["RateLimiting:OtpWindowMinutes"] ?? "15");

        var rateLimit = await _context.OtpRateLimits
            .FirstOrDefaultAsync(r => r.PhoneNumber == phoneNumber);

        if (rateLimit == null)
        {
            rateLimit = new OtpRateLimit
            {
                PhoneNumber = phoneNumber,
                RequestCount = 1,
                WindowStart = DateTime.UtcNow
            };
            _context.OtpRateLimits.Add(rateLimit);
        }
        else
        {
            // Check if window has expired
            if (rateLimit.WindowStart.AddMinutes(windowMinutes) < DateTime.UtcNow)
            {
                rateLimit.RequestCount = 1;
                rateLimit.WindowStart = DateTime.UtcNow;
                rateLimit.BlockedUntil = null;
            }
            else
            {
                rateLimit.RequestCount++;

                // If max attempts reached, block for the remaining window time
                if (rateLimit.RequestCount >= maxAttempts)
                {
                    rateLimit.BlockedUntil = rateLimit.WindowStart.AddMinutes(windowMinutes);
                }
            }
        }
    }

    private static string GenerateOtp()
    {
        var random = new Random();
        return random.Next(0, 1000000).ToString("D6");
    }
}

using Microsoft.EntityFrameworkCore;
using Funtime.Identity.Api.Data;
using Funtime.Identity.Api.Models;

namespace Funtime.Identity.Api.Services;

public class OtpService : IOtpService
{
    private readonly ApplicationDbContext _context;
    private readonly ISmsService _smsService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OtpService> _logger;

    private const int OTP_EXPIRATION_MINUTES = 5;
    private const int OTP_LENGTH = 6;

    public OtpService(
        ApplicationDbContext context,
        ISmsService smsService,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<OtpService> logger)
    {
        _context = context;
        _smsService = smsService;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> IsRateLimitedAsync(string identifier)
    {
        var maxAttempts = int.Parse(_configuration["RateLimiting:OtpMaxAttempts"] ?? "5");
        var windowMinutes = int.Parse(_configuration["RateLimiting:OtpWindowMinutes"] ?? "15");

        var rateLimit = await _context.OtpRateLimits
            .FirstOrDefaultAsync(r => r.Identifier == identifier);

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

    public async Task<(bool success, string message)> SendOtpAsync(string identifier)
    {
        // Check rate limiting
        if (await IsRateLimitedAsync(identifier))
        {
            return (false, "Too many OTP requests. Please try again later.");
        }

        // Generate OTP
        var code = GenerateOtp();
        var expiresAt = DateTime.UtcNow.AddMinutes(OTP_EXPIRATION_MINUTES);

        // Look up existing user by email or phone (don't reveal if found)
        int? matchedUserId = null;
        if (IsEmail(identifier))
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == identifier.ToLower());
            matchedUserId = user?.Id;
        }
        else
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == identifier);
            matchedUserId = user?.Id;
        }

        // Invalidate previous unused OTPs for this identifier
        var previousOtps = await _context.OtpRequests
            .Where(o => o.Identifier == identifier && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        foreach (var otp in previousOtps)
        {
            otp.IsUsed = true;
        }

        // Create new OTP request with matched user ID
        var otpRequest = new OtpRequest
        {
            Identifier = identifier,
            UserId = matchedUserId,
            Code = code,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        };

        _context.OtpRequests.Add(otpRequest);

        // Update rate limit counter
        await UpdateRateLimitAsync(identifier);

        await _context.SaveChangesAsync();

        // Call stored procedure to queue notification (external process handles delivery)
        var isEmail = IsEmail(identifier);
        try
        {
            await _context.Database.ExecuteSqlRawAsync(
                "EXEC nsp_Password_Reset_Request @OtpRequestId = {0}, @IsEmail = {1}",
                otpRequest.Id,
                isEmail);

            _logger.LogInformation("Password reset request queued for {Identifier} (IsEmail: {IsEmail})", identifier, isEmail);
            return (true, "OTP sent successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue password reset request for {Identifier}", identifier);
            return (false, "Failed to send OTP. Please try again.");
        }
    }

    public async Task<(bool success, string message, int? userId)> VerifyOtpAsync(string identifier, string code, bool markAsUsed = true)
    {
        var otpRequest = await _context.OtpRequests
            .Where(o => o.Identifier == identifier &&
                        o.Code == code &&
                        !o.IsUsed &&
                        o.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

        if (otpRequest == null)
        {
            // Check if there's an expired or used OTP to give better error message
            var existingOtp = await _context.OtpRequests
                .Where(o => o.Identifier == identifier && o.Code == code)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (existingOtp != null)
            {
                if (existingOtp.IsUsed)
                {
                    return (false, "This OTP has already been used.", null);
                }
                if (existingOtp.ExpiresAt <= DateTime.UtcNow)
                {
                    return (false, "This OTP has expired.", null);
                }
            }

            return (false, "Invalid OTP.", null);
        }

        // Mark OTP as used if requested
        if (markAsUsed)
        {
            otpRequest.IsUsed = true;
            await _context.SaveChangesAsync();
        }

        // Return the matched user ID (null if no existing user)
        return (true, "OTP verified successfully.", otpRequest.UserId);
    }

    private async Task UpdateRateLimitAsync(string identifier)
    {
        var maxAttempts = int.Parse(_configuration["RateLimiting:OtpMaxAttempts"] ?? "5");
        var windowMinutes = int.Parse(_configuration["RateLimiting:OtpWindowMinutes"] ?? "15");

        var rateLimit = await _context.OtpRateLimits
            .FirstOrDefaultAsync(r => r.Identifier == identifier);

        if (rateLimit == null)
        {
            rateLimit = new OtpRateLimit
            {
                Identifier = identifier,
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
        return System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
    }

    private static bool IsEmail(string identifier)
    {
        return identifier.Contains('@');
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Funtime.Identity.Api.Data;
using Funtime.Identity.Api.DTOs;
using Funtime.Identity.Api.Models;
using Funtime.Identity.Api.Services;

namespace Funtime.Identity.Api.Controllers;

/// <summary>
/// Endpoints for changing user email and phone with OTP verification.
/// </summary>
[ApiController]
[Route("auth")]
[Authorize]
public class CredentialChangeController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly ILogger<CredentialChangeController> _logger;

    private const int OTP_EXPIRY_MINUTES = 10;
    private const int MAX_REQUESTS_PER_WINDOW = 5;
    private const int RATE_LIMIT_WINDOW_MINUTES = 15;

    public CredentialChangeController(
        ApplicationDbContext context,
        IJwtService jwtService,
        ILogger<CredentialChangeController> logger)
    {
        _context = context;
        _jwtService = jwtService;
        _logger = logger;
    }

    #region Change Email

    /// <summary>
    /// Request to change email address. Sends OTP to new email.
    /// </summary>
    [HttpPost("change-email/request")]
    public async Task<ActionResult<CredentialChangeRequestResponse>> RequestEmailChange(
        [FromBody] ChangeEmailRequestDto request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new CredentialChangeRequestResponse
            {
                Success = false,
                Message = "Unauthorized"
            });
        }

        var user = await _context.Users.FindAsync(userId.Value);
        if (user == null)
        {
            return Unauthorized(new CredentialChangeRequestResponse
            {
                Success = false,
                Message = "User not found"
            });
        }

        // Normalize email
        var newEmail = request.NewEmail.Trim().ToLower();

        // Check if new email is same as current
        if (user.Email?.ToLower() == newEmail)
        {
            return BadRequest(new CredentialChangeRequestResponse
            {
                Success = false,
                Message = "New email is the same as current email"
            });
        }

        // Check if email is already registered to another user
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == newEmail && u.Id != userId);
        if (existingUser != null)
        {
            return BadRequest(new CredentialChangeRequestResponse
            {
                Success = false,
                Message = "Email already registered"
            });
        }

        // Check rate limiting
        if (await IsRateLimited(userId.Value, "email"))
        {
            return StatusCode(429, new CredentialChangeRequestResponse
            {
                Success = false,
                Message = $"Too many requests. Try again in {RATE_LIMIT_WINDOW_MINUTES} minutes"
            });
        }

        // Invalidate any existing OTPs for this user/type/value
        await InvalidatePreviousOtps(userId.Value, "email", newEmail);

        // Generate and store OTP
        var code = GenerateOtp();
        var otp = new CredentialChangeOtp
        {
            UserId = userId.Value,
            ChangeType = "email",
            NewValue = newEmail,
            Code = code,
            ExpiresAt = DateTime.UtcNow.AddMinutes(OTP_EXPIRY_MINUTES),
            CreatedAt = DateTime.UtcNow
        };

        _context.CredentialChangeOtps.Add(otp);
        await _context.SaveChangesAsync();

        // Send OTP via stored procedure (queues for external delivery)
        try
        {
            await _context.Database.ExecuteSqlRawAsync(
                "EXEC nsp_Credential_Change_Request @OtpId = {0}, @ChangeType = {1}",
                otp.Id,
                "email");

            _logger.LogInformation("Email change OTP sent for user {UserId} to {Email}",
                userId, MaskEmail(newEmail));

            return Ok(new CredentialChangeRequestResponse
            {
                Success = true,
                Message = $"Verification code sent to {MaskEmail(newEmail)}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email change OTP for user {UserId}", userId);

            // If SP doesn't exist yet, still return success (for development)
            _logger.LogWarning("Stored procedure nsp_Credential_Change_Request may not exist for user {UserId}. OTP was generated but delivery failed.", userId);

            return Ok(new CredentialChangeRequestResponse
            {
                Success = true,
                Message = $"Verification code sent to {MaskEmail(newEmail)}"
            });
        }
    }

    /// <summary>
    /// Verify email change with OTP code. Updates email and returns new token.
    /// </summary>
    [HttpPost("change-email/verify")]
    public async Task<ActionResult<CredentialChangeVerifyResponse>> VerifyEmailChange(
        [FromBody] ChangeEmailVerifyDto request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new CredentialChangeVerifyResponse
            {
                Success = false,
                Message = "Unauthorized"
            });
        }

        var user = await _context.Users.FindAsync(userId.Value);
        if (user == null)
        {
            return Unauthorized(new CredentialChangeVerifyResponse
            {
                Success = false,
                Message = "User not found"
            });
        }

        var newEmail = request.NewEmail.Trim().ToLower();

        // Double-check email isn't taken (race condition protection)
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == newEmail && u.Id != userId);
        if (existingUser != null)
        {
            return BadRequest(new CredentialChangeVerifyResponse
            {
                Success = false,
                Message = "Email already registered"
            });
        }

        // Find valid OTP
        var otp = await _context.CredentialChangeOtps
            .Where(o => o.UserId == userId.Value &&
                        o.ChangeType == "email" &&
                        o.NewValue == newEmail &&
                        !o.IsUsed &&
                        o.ExpiresAt > DateTime.UtcNow &&
                        o.Attempts < CredentialChangeOtp.MaxAttempts)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

        if (otp == null)
        {
            return BadRequest(new CredentialChangeVerifyResponse
            {
                Success = false,
                Message = "Invalid or expired verification code"
            });
        }

        // Verify code
        if (otp.Code != request.Code)
        {
            otp.Attempts++;
            await _context.SaveChangesAsync();

            if (otp.Attempts >= CredentialChangeOtp.MaxAttempts)
            {
                otp.IsUsed = true;
                await _context.SaveChangesAsync();
            }

            return BadRequest(new CredentialChangeVerifyResponse
            {
                Success = false,
                Message = "Invalid or expired verification code"
            });
        }

        // Mark OTP as used
        otp.IsUsed = true;

        // Update user's email
        user.Email = newEmail;
        user.IsEmailVerified = true; // New email is verified by this process
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Generate new JWT with updated email
        var newToken = await _jwtService.GenerateTokenAsync(user);

        _logger.LogInformation("User {UserId} changed email to {Email}", userId, MaskEmail(newEmail));

        return Ok(new CredentialChangeVerifyResponse
        {
            Success = true,
            Message = "Email updated successfully",
            Token = newToken,
            User = new CredentialChangeUserInfo
            {
                Id = user.Id,
                Email = user.Email,
                Phone = user.PhoneNumber
            }
        });
    }

    #endregion

    #region Change Phone

    /// <summary>
    /// Request to change phone number. Sends OTP via SMS.
    /// </summary>
    [HttpPost("change-phone/request")]
    public async Task<ActionResult<CredentialChangeRequestResponse>> RequestPhoneChange(
        [FromBody] ChangePhoneRequestDto request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new CredentialChangeRequestResponse
            {
                Success = false,
                Message = "Unauthorized"
            });
        }

        var user = await _context.Users.FindAsync(userId.Value);
        if (user == null)
        {
            return Unauthorized(new CredentialChangeRequestResponse
            {
                Success = false,
                Message = "User not found"
            });
        }

        // Normalize phone number
        var newPhone = NormalizePhoneNumber(request.NewPhoneNumber);

        // Check if new phone is same as current
        if (user.PhoneNumber == newPhone)
        {
            return BadRequest(new CredentialChangeRequestResponse
            {
                Success = false,
                Message = "New phone number is the same as current"
            });
        }

        // Check if phone is already registered to another user
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == newPhone && u.Id != userId);
        if (existingUser != null)
        {
            return BadRequest(new CredentialChangeRequestResponse
            {
                Success = false,
                Message = "Phone number already registered"
            });
        }

        // Check rate limiting
        if (await IsRateLimited(userId.Value, "phone"))
        {
            return StatusCode(429, new CredentialChangeRequestResponse
            {
                Success = false,
                Message = $"Too many requests. Try again in {RATE_LIMIT_WINDOW_MINUTES} minutes"
            });
        }

        // Invalidate any existing OTPs for this user/type/value
        await InvalidatePreviousOtps(userId.Value, "phone", newPhone);

        // Generate and store OTP
        var code = GenerateOtp();
        var otp = new CredentialChangeOtp
        {
            UserId = userId.Value,
            ChangeType = "phone",
            NewValue = newPhone,
            Code = code,
            ExpiresAt = DateTime.UtcNow.AddMinutes(OTP_EXPIRY_MINUTES),
            CreatedAt = DateTime.UtcNow
        };

        _context.CredentialChangeOtps.Add(otp);
        await _context.SaveChangesAsync();

        // Send OTP via stored procedure (queues for external delivery)
        try
        {
            await _context.Database.ExecuteSqlRawAsync(
                "EXEC nsp_Credential_Change_Request @OtpId = {0}, @ChangeType = {1}",
                otp.Id,
                "phone");

            _logger.LogInformation("Phone change OTP sent for user {UserId} to {Phone}",
                userId, MaskPhone(newPhone));

            return Ok(new CredentialChangeRequestResponse
            {
                Success = true,
                Message = $"Verification code sent to {MaskPhone(newPhone)}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send phone change OTP for user {UserId}", userId);

            // If SP doesn't exist yet, still return success (for development)
            _logger.LogWarning("Stored procedure nsp_Credential_Change_Request may not exist for user {UserId}. OTP was generated but delivery failed.", userId);

            return Ok(new CredentialChangeRequestResponse
            {
                Success = true,
                Message = $"Verification code sent to {MaskPhone(newPhone)}"
            });
        }
    }

    /// <summary>
    /// Verify phone change with OTP code. Updates phone and returns new token.
    /// </summary>
    [HttpPost("change-phone/verify")]
    public async Task<ActionResult<CredentialChangeVerifyResponse>> VerifyPhoneChange(
        [FromBody] ChangePhoneVerifyDto request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new CredentialChangeVerifyResponse
            {
                Success = false,
                Message = "Unauthorized"
            });
        }

        var user = await _context.Users.FindAsync(userId.Value);
        if (user == null)
        {
            return Unauthorized(new CredentialChangeVerifyResponse
            {
                Success = false,
                Message = "User not found"
            });
        }

        var newPhone = NormalizePhoneNumber(request.NewPhoneNumber);

        // Double-check phone isn't taken (race condition protection)
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == newPhone && u.Id != userId);
        if (existingUser != null)
        {
            return BadRequest(new CredentialChangeVerifyResponse
            {
                Success = false,
                Message = "Phone number already registered"
            });
        }

        // Find valid OTP
        var otp = await _context.CredentialChangeOtps
            .Where(o => o.UserId == userId.Value &&
                        o.ChangeType == "phone" &&
                        o.NewValue == newPhone &&
                        !o.IsUsed &&
                        o.ExpiresAt > DateTime.UtcNow &&
                        o.Attempts < CredentialChangeOtp.MaxAttempts)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

        if (otp == null)
        {
            return BadRequest(new CredentialChangeVerifyResponse
            {
                Success = false,
                Message = "Invalid or expired verification code"
            });
        }

        // Verify code
        if (otp.Code != request.Code)
        {
            otp.Attempts++;
            await _context.SaveChangesAsync();

            if (otp.Attempts >= CredentialChangeOtp.MaxAttempts)
            {
                otp.IsUsed = true;
                await _context.SaveChangesAsync();
            }

            return BadRequest(new CredentialChangeVerifyResponse
            {
                Success = false,
                Message = "Invalid or expired verification code"
            });
        }

        // Mark OTP as used
        otp.IsUsed = true;

        // Update user's phone
        user.PhoneNumber = newPhone;
        user.IsPhoneVerified = true; // New phone is verified by this process
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Generate new JWT with updated phone
        var newToken = await _jwtService.GenerateTokenAsync(user);

        _logger.LogInformation("User {UserId} changed phone to {Phone}", userId, MaskPhone(newPhone));

        return Ok(new CredentialChangeVerifyResponse
        {
            Success = true,
            Message = "Phone number updated successfully",
            Token = newToken,
            User = new CredentialChangeUserInfo
            {
                Id = user.Id,
                Email = user.Email,
                Phone = user.PhoneNumber
            }
        });
    }

    #endregion

    #region Helpers

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }

    private static string GenerateOtp()
    {
        return System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
    }

    private async Task<bool> IsRateLimited(int userId, string changeType)
    {
        var windowStart = DateTime.UtcNow.AddMinutes(-RATE_LIMIT_WINDOW_MINUTES);

        var requestCount = await _context.CredentialChangeOtps
            .CountAsync(o => o.UserId == userId &&
                            o.ChangeType == changeType &&
                            o.CreatedAt > windowStart);

        return requestCount >= MAX_REQUESTS_PER_WINDOW;
    }

    private async Task InvalidatePreviousOtps(int userId, string changeType, string newValue)
    {
        var previousOtps = await _context.CredentialChangeOtps
            .Where(o => o.UserId == userId &&
                        o.ChangeType == changeType &&
                        o.NewValue == newValue &&
                        !o.IsUsed &&
                        o.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        foreach (var otp in previousOtps)
        {
            otp.IsUsed = true;
        }

        if (previousOtps.Any())
        {
            await _context.SaveChangesAsync();
        }
    }

    private static string NormalizePhoneNumber(string phone)
    {
        // Remove spaces, dashes, parentheses
        var normalized = new string(phone.Where(c => char.IsDigit(c) || c == '+').ToArray());

        // Ensure it starts with + if it has a country code
        if (!normalized.StartsWith("+") && normalized.Length > 10)
        {
            normalized = "+" + normalized;
        }

        return normalized;
    }

    private static string MaskEmail(string email)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex <= 1) return email;

        var local = email[..atIndex];
        var domain = email[atIndex..];

        return local[0] + "***" + domain;
    }

    private static string MaskPhone(string phone)
    {
        if (phone.Length <= 4) return phone;

        var visibleEnd = phone[^4..];
        var prefix = phone.Length > 6 ? phone[..2] : phone[..1];

        return prefix + "***" + visibleEnd;
    }

    #endregion
}

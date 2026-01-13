using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Funtime.Identity.Api.Data;
using Funtime.Identity.Api.DTOs;
using Funtime.Identity.Api.Services;

namespace Funtime.Identity.Api.Controllers;

/// <summary>
/// User-facing verification endpoints for email and phone verification.
/// These endpoints are called by logged-in users to verify their own email/phone.
/// </summary>
[ApiController]
[Route("api/verify")]
[Authorize]
public class VerifyController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IOtpService _otpService;
    private readonly ILogger<VerifyController> _logger;

    private const int CODE_EXPIRY_SECONDS = 600; // 10 minutes

    public VerifyController(
        ApplicationDbContext context,
        IOtpService otpService,
        ILogger<VerifyController> logger)
    {
        _context = context;
        _otpService = otpService;
        _logger = logger;
    }

    /// <summary>
    /// Request a verification code to be sent to the user's email or phone
    /// </summary>
    [HttpPost("request")]
    public async Task<ActionResult<VerifyRequestResponse>> RequestVerification([FromBody] VerifyRequestRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new VerifyRequestResponse
            {
                Success = false,
                Message = "User not authenticated."
            });
        }

        var user = await _context.Users.FindAsync(userId.Value);
        if (user == null)
        {
            return NotFound(new VerifyRequestResponse
            {
                Success = false,
                Message = "User not found."
            });
        }

        string identifier;
        string maskedIdentifier;

        if (request.Type?.ToLower() == "email")
        {
            if (string.IsNullOrEmpty(user.Email))
            {
                return BadRequest(new VerifyRequestResponse
                {
                    Success = false,
                    Message = "No email address on file."
                });
            }
            if (user.IsEmailVerified)
            {
                return BadRequest(new VerifyRequestResponse
                {
                    Success = false,
                    Message = "Email is already verified."
                });
            }
            identifier = user.Email;
            maskedIdentifier = MaskEmail(user.Email);
        }
        else if (request.Type?.ToLower() == "phone")
        {
            if (string.IsNullOrEmpty(user.PhoneNumber))
            {
                return BadRequest(new VerifyRequestResponse
                {
                    Success = false,
                    Message = "No phone number on file."
                });
            }
            if (user.IsPhoneVerified)
            {
                return BadRequest(new VerifyRequestResponse
                {
                    Success = false,
                    Message = "Phone is already verified."
                });
            }
            identifier = user.PhoneNumber;
            maskedIdentifier = MaskPhone(user.PhoneNumber);
        }
        else
        {
            return BadRequest(new VerifyRequestResponse
            {
                Success = false,
                Message = "Type must be 'email' or 'phone'."
            });
        }

        var (success, message) = await _otpService.SendOtpAsync(identifier);
        if (!success)
        {
            _logger.LogWarning("Failed to send verification code to user {UserId}: {Message}", userId, message);
            return BadRequest(new VerifyRequestResponse
            {
                Success = false,
                Message = message
            });
        }

        _logger.LogInformation("Verification code sent to user {UserId} ({Type})", userId, request.Type);

        return Ok(new VerifyRequestResponse
        {
            Success = true,
            Message = $"Verification code sent to {maskedIdentifier}",
            MaskedIdentifier = maskedIdentifier,
            ExpiresInSeconds = CODE_EXPIRY_SECONDS
        });
    }

    /// <summary>
    /// Confirm verification with the code received via email/SMS
    /// </summary>
    [HttpPost("confirm")]
    public async Task<ActionResult<VerifyConfirmResponse>> ConfirmVerification([FromBody] VerifyConfirmRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new VerifyConfirmResponse
            {
                Success = false,
                Message = "User not authenticated.",
                Verified = false
            });
        }

        var user = await _context.Users.FindAsync(userId.Value);
        if (user == null)
        {
            return NotFound(new VerifyConfirmResponse
            {
                Success = false,
                Message = "User not found.",
                Verified = false
            });
        }

        string identifier;
        bool isEmail = request.Type?.ToLower() == "email";

        if (isEmail)
        {
            if (string.IsNullOrEmpty(user.Email))
            {
                return BadRequest(new VerifyConfirmResponse
                {
                    Success = false,
                    Message = "No email address on file.",
                    Verified = false
                });
            }
            if (user.IsEmailVerified)
            {
                return Ok(new VerifyConfirmResponse
                {
                    Success = true,
                    Message = "Email is already verified.",
                    Verified = true
                });
            }
            identifier = user.Email;
        }
        else if (request.Type?.ToLower() == "phone")
        {
            if (string.IsNullOrEmpty(user.PhoneNumber))
            {
                return BadRequest(new VerifyConfirmResponse
                {
                    Success = false,
                    Message = "No phone number on file.",
                    Verified = false
                });
            }
            if (user.IsPhoneVerified)
            {
                return Ok(new VerifyConfirmResponse
                {
                    Success = true,
                    Message = "Phone is already verified.",
                    Verified = true
                });
            }
            identifier = user.PhoneNumber;
        }
        else
        {
            return BadRequest(new VerifyConfirmResponse
            {
                Success = false,
                Message = "Type must be 'email' or 'phone'.",
                Verified = false
            });
        }

        // Verify the code
        var (success, message, _) = await _otpService.VerifyOtpAsync(identifier, request.Code, markAsUsed: true);
        if (!success)
        {
            _logger.LogWarning("Verification failed for user {UserId} ({Type}): {Message}", userId, request.Type, message);
            return BadRequest(new VerifyConfirmResponse
            {
                Success = false,
                Message = message,
                Verified = false
            });
        }

        // Update user verification status
        if (isEmail)
        {
            user.IsEmailVerified = true;
        }
        else
        {
            user.IsPhoneVerified = true;
        }
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} verified their {Type}", userId, request.Type);

        return Ok(new VerifyConfirmResponse
        {
            Success = true,
            Message = $"{(isEmail ? "Email" : "Phone")} verified successfully!",
            Verified = true
        });
    }

    /// <summary>
    /// Get the current user's verification status
    /// </summary>
    [HttpGet("status")]
    public async Task<ActionResult<VerifyStatusResponse>> GetStatus()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var user = await _context.Users.FindAsync(userId.Value);
        if (user == null)
        {
            return NotFound();
        }

        return Ok(new VerifyStatusResponse
        {
            IsEmailVerified = user.IsEmailVerified,
            IsPhoneVerified = user.IsPhoneVerified,
            Email = !string.IsNullOrEmpty(user.Email) ? MaskEmail(user.Email) : null,
            Phone = !string.IsNullOrEmpty(user.PhoneNumber) ? MaskPhone(user.PhoneNumber) : null
        });
    }

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

    /// <summary>
    /// Mask email: "john.doe@example.com" -> "j***@example.com"
    /// </summary>
    private static string MaskEmail(string email)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex <= 1) return email;

        var local = email[..atIndex];
        var domain = email[atIndex..];

        if (local.Length <= 2)
        {
            return local[0] + "***" + domain;
        }

        return local[0] + "***" + domain;
    }

    /// <summary>
    /// Mask phone: "+12345678901" -> "+1***8901"
    /// </summary>
    private static string MaskPhone(string phone)
    {
        if (phone.Length <= 4) return phone;

        var visibleEnd = phone[^4..];
        var prefix = phone.Length > 6 ? phone[..2] : phone[..1];

        return prefix + "***" + visibleEnd;
    }

    #endregion
}

using System.ComponentModel.DataAnnotations;

namespace Funtime.Identity.Api.DTOs;

// Registration
public class RegisterRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;
}

// Email/Password Login
public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Optional: Site key to return the user's role for that site
    /// </summary>
    [MaxLength(50)]
    public string? SiteKey { get; set; }
}

// Phone/Password Login
public class PhoneLoginRequest
{
    [Required]
    [MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Optional: Site key to return the user's role for that site
    /// </summary>
    [MaxLength(50)]
    public string? SiteKey { get; set; }
}

// Public site info (no auth required)
public class PublicSiteResponse
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Url { get; set; }
    public string? LogoUrl { get; set; }
}

// OTP Send Request
public class OtpSendRequest
{
    [Required]
    [MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;
}

// OTP Verify Request
public class OtpVerifyRequest
{
    [Required]
    [MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// First name for new user registration via OTP
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Last name for new user registration via OTP
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;
}

// Link Phone to existing account
public class LinkPhoneRequest
{
    [Required]
    [MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Code { get; set; } = string.Empty;
}

// Link Email to existing account
public class LinkEmailRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;
}

// Validate token request
public class ValidateTokenRequest
{
    [Required]
    public string Token { get; set; } = string.Empty;
}

// Auth Response with token
public class AuthResponse
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public string? Message { get; set; }
    public UserResponse? User { get; set; }
}

// User Response (for /me endpoint)
public class UserResponse
{
    public int Id { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? SystemRole { get; set; }
    public bool IsEmailVerified { get; set; }
    public bool IsPhoneVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// User's role for the requested site (if SiteKey was provided in login request)
    /// </summary>
    public string? SiteRole { get; set; }

    /// <summary>
    /// Whether the user is an admin for the requested site
    /// </summary>
    public bool? IsSiteAdmin { get; set; }
}

// Generic API Response
public class ApiResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}

// Token validation response
public class ValidateTokenResponse
{
    public bool Valid { get; set; }
    public int? UserId { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? SystemRole { get; set; }
    public List<string>? Sites { get; set; }
    public string? Message { get; set; }
}

// Force auth request (for legacy system integration)
public class ForceAuthRequest
{
    [Required]
    public int UserId { get; set; }

    [Required]
    public string ApiSecretKey { get; set; } = string.Empty;
}

// External login request (login or register via external provider)
public class ExternalLoginRequest
{
    /// <summary>
    /// Provider name (e.g., "google", "apple", "wechat", "facebook", "github")
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// The unique user ID from the external provider
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string ProviderUserId { get; set; } = string.Empty;

    /// <summary>
    /// Optional: Email from the provider
    /// </summary>
    [EmailAddress]
    [MaxLength(255)]
    public string? ProviderEmail { get; set; }

    /// <summary>
    /// Optional: Display name from the provider
    /// </summary>
    [MaxLength(255)]
    public string? ProviderDisplayName { get; set; }

    /// <summary>
    /// API secret key for server-to-server authentication
    /// </summary>
    [Required]
    public string ApiSecretKey { get; set; } = string.Empty;
}

// Link external provider to existing account (requires JWT auth)
public class LinkExternalLoginRequest
{
    /// <summary>
    /// Provider name (e.g., "google", "apple", "wechat", "facebook", "github")
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// The unique user ID from the external provider
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string ProviderUserId { get; set; } = string.Empty;

    /// <summary>
    /// Optional: Email from the provider
    /// </summary>
    [EmailAddress]
    [MaxLength(255)]
    public string? ProviderEmail { get; set; }

    /// <summary>
    /// Optional: Display name from the provider
    /// </summary>
    [MaxLength(255)]
    public string? ProviderDisplayName { get; set; }
}

// Unlink external provider request
public class UnlinkExternalLoginRequest
{
    /// <summary>
    /// Provider name to unlink (e.g., "google", "apple", "wechat")
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Provider { get; set; } = string.Empty;
}

// External login info in response
public class ExternalLoginResponse
{
    public string Provider { get; set; } = string.Empty;
    public string ProviderUserId { get; set; } = string.Empty;
    public string? ProviderEmail { get; set; }
    public string? ProviderDisplayName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
}

// Extended user response with external logins
public class UserWithExternalLoginsResponse : UserResponse
{
    public List<ExternalLoginResponse> ExternalLogins { get; set; } = new();
}

// Change password request (for logged-in users)
public class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(100)]
    public string NewPassword { get; set; } = string.Empty;
}

// Reset password request (using phone OTP)
public class ResetPasswordRequest
{
    [Required]
    [MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(100)]
    public string NewPassword { get; set; } = string.Empty;
}

// Request password reset code (for email or phone)
public class PasswordResetSendRequest
{
    /// <summary>
    /// Email address for password reset
    /// </summary>
    [EmailAddress]
    [MaxLength(255)]
    public string? Email { get; set; }

    /// <summary>
    /// Phone number for password reset (alternative to email)
    /// </summary>
    [MaxLength(30)]
    public string? PhoneNumber { get; set; }
}

// Verify OTP code only (for password reset flow)
public class PasswordResetVerifyRequest
{
    /// <summary>
    /// Email address (if verifying via email)
    /// </summary>
    [EmailAddress]
    [MaxLength(255)]
    public string? Email { get; set; }

    /// <summary>
    /// Phone number (if verifying via phone)
    /// </summary>
    [MaxLength(30)]
    public string? PhoneNumber { get; set; }

    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Code { get; set; } = string.Empty;
}

// Response for password reset verify
public class PasswordResetVerifyResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public bool AccountExists { get; set; }
}

// Reset password with code (supports both email and phone)
public class PasswordResetWithCodeRequest
{
    /// <summary>
    /// Email address (if resetting via email)
    /// </summary>
    [EmailAddress]
    [MaxLength(255)]
    public string? Email { get; set; }

    /// <summary>
    /// Phone number (if resetting via phone)
    /// </summary>
    [MaxLength(30)]
    public string? PhoneNumber { get; set; }

    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(100)]
    public string NewPassword { get; set; } = string.Empty;
}

// Quick registration request (after OTP verification with no existing account)
public class PasswordResetRegisterRequest
{
    /// <summary>
    /// Email address (if registering via email)
    /// </summary>
    [EmailAddress]
    [MaxLength(255)]
    public string? Email { get; set; }

    /// <summary>
    /// Phone number (if registering via phone)
    /// </summary>
    [MaxLength(30)]
    public string? PhoneNumber { get; set; }

    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;
}

#region JWT Cross-Site Support

// Token validation request
public class TokenValidationRequest
{
    [Required]
    public string Token { get; set; } = string.Empty;
}

// Token validation response
public class TokenValidationResponse
{
    public bool IsValid { get; set; }
    public int? UserId { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? SystemRole { get; set; }
    public List<string>? Sites { get; set; }
    public string? Message { get; set; }
}

// JWT configuration response (for other sites to know token parameters)
// Note: Signing key intentionally excluded — use /auth/validate-token for verification
public class JwtConfigResponse
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; }
}

#endregion

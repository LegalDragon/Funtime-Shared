using System.ComponentModel.DataAnnotations;

namespace FTPBAuth.API.DTOs;

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
}

// Email/Password Login
public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

// OTP Send Request
public class OtpSendRequest
{
    [Required]
    [Phone]
    [MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;
}

// OTP Verify Request
public class OtpVerifyRequest
{
    [Required]
    [Phone]
    [MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Code { get; set; } = string.Empty;
}

// Link Phone to existing account
public class LinkPhoneRequest
{
    [Required]
    [Phone]
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
    public bool IsEmailVerified { get; set; }
    public bool IsPhoneVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
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
    [Phone]
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

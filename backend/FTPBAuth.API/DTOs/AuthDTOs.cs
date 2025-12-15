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

using System.ComponentModel.DataAnnotations;

namespace Funtime.Identity.Api.DTOs;

#region Verification

public class VerifyRequestRequest
{
    /// <summary>
    /// Type of verification: "email" or "phone"
    /// </summary>
    [Required]
    public string Type { get; set; } = string.Empty;
}

public class VerifyRequestResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    /// <summary>
    /// Masked identifier (e.g., "j***@example.com" or "+1***567890")
    /// </summary>
    public string? MaskedIdentifier { get; set; }
    /// <summary>
    /// Seconds until code expires
    /// </summary>
    public int ExpiresInSeconds { get; set; }
}

public class VerifyConfirmRequest
{
    /// <summary>
    /// Type of verification: "email" or "phone"
    /// </summary>
    [Required]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// The 6-digit verification code
    /// </summary>
    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Code { get; set; } = string.Empty;
}

public class VerifyConfirmResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool Verified { get; set; }
}

public class VerifyStatusResponse
{
    public bool IsEmailVerified { get; set; }
    public bool IsPhoneVerified { get; set; }
    /// <summary>
    /// Masked email (e.g., "j***@example.com")
    /// </summary>
    public string? Email { get; set; }
    /// <summary>
    /// Masked phone (e.g., "+1***567890")
    /// </summary>
    public string? Phone { get; set; }
}

#endregion

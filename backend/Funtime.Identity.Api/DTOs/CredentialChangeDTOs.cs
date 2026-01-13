using System.ComponentModel.DataAnnotations;

namespace Funtime.Identity.Api.DTOs;

#region Change Email

public class ChangeEmailRequestDto
{
    /// <summary>
    /// The new email address to change to
    /// </summary>
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string NewEmail { get; set; } = string.Empty;
}

public class ChangeEmailVerifyDto
{
    /// <summary>
    /// The new email address
    /// </summary>
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string NewEmail { get; set; } = string.Empty;

    /// <summary>
    /// The 6-digit verification code sent to the new email
    /// </summary>
    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Code { get; set; } = string.Empty;
}

#endregion

#region Change Phone

public class ChangePhoneRequestDto
{
    /// <summary>
    /// The new phone number to change to (E.164 format recommended, e.g., +12025551234)
    /// </summary>
    [Required]
    [Phone]
    [MaxLength(20)]
    public string NewPhoneNumber { get; set; } = string.Empty;
}

public class ChangePhoneVerifyDto
{
    /// <summary>
    /// The new phone number
    /// </summary>
    [Required]
    [Phone]
    [MaxLength(20)]
    public string NewPhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// The 6-digit verification code sent via SMS
    /// </summary>
    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Code { get; set; } = string.Empty;
}

#endregion

#region Responses

public class CredentialChangeRequestResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class CredentialChangeVerifyResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// New JWT token with updated claims (email or phone)
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// Updated user info
    /// </summary>
    public CredentialChangeUserInfo? User { get; set; }
}

public class CredentialChangeUserInfo
{
    public int Id { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
}

#endregion

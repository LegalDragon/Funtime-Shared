using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Funtime.Identity.Api.Models;

/// <summary>
/// Stores OTP codes for email/phone change requests.
/// Separate from OtpRequest to handle the unique flow of changing credentials.
/// </summary>
public class CredentialChangeOtp
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// The user requesting the credential change
    /// </summary>
    public int UserId { get; set; }

    [ForeignKey("UserId")]
    public User? User { get; set; }

    /// <summary>
    /// Type of credential change: "email" or "phone"
    /// </summary>
    [Required]
    [MaxLength(10)]
    public string ChangeType { get; set; } = string.Empty;

    /// <summary>
    /// The new email or phone number to change to
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string NewValue { get; set; } = string.Empty;

    /// <summary>
    /// The 6-digit OTP code
    /// </summary>
    [Required]
    [MaxLength(6)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// When this OTP expires
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Number of verification attempts made
    /// </summary>
    public int Attempts { get; set; } = 0;

    /// <summary>
    /// Maximum allowed attempts before OTP is invalidated
    /// </summary>
    public const int MaxAttempts = 5;

    /// <summary>
    /// When this OTP was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether this OTP has been used or invalidated
    /// </summary>
    public bool IsUsed { get; set; } = false;
}

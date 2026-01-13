using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Funtime.Identity.Api.Models;

public class OtpRequest
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// The identifier (email or phone number) for OTP delivery
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string Identifier { get; set; } = string.Empty;

    /// <summary>
    /// The matched user ID if a user exists with this identifier (null if no match)
    /// </summary>
    public int? UserId { get; set; }

    [ForeignKey("UserId")]
    public User? User { get; set; }

    [Required]
    [MaxLength(6)]
    public string Code { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; }

    public bool IsUsed { get; set; } = false;

    public int AttemptCount { get; set; } = 0;
}

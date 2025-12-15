using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FTPBAuth.API.Models;

public class ExternalLogin
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [ForeignKey("UserId")]
    public User User { get; set; } = null!;

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
    /// Optional: Email from the provider (may differ from user's primary email)
    /// </summary>
    [MaxLength(255)]
    public string? ProviderEmail { get; set; }

    /// <summary>
    /// Optional: Display name from the provider
    /// </summary>
    [MaxLength(255)]
    public string? ProviderDisplayName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastUsedAt { get; set; }
}

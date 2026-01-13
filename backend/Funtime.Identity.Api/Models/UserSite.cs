using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Funtime.Identity.Api.Models;

/// <summary>
/// Tracks which pickleball.* sites each user has joined
/// </summary>
public class UserSite
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [ForeignKey("UserId")]
    public User User { get; set; } = null!;

    /// <summary>
    /// Site identifier (e.g., "community", "college", "date", "jobs")
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string SiteKey { get; set; } = string.Empty;

    /// <summary>
    /// When the user joined this site
    /// </summary>
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the user is active on this site
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Optional role on this site (e.g., "member", "admin", "moderator")
    /// </summary>
    [MaxLength(50)]
    public string Role { get; set; } = "member";
}

using System.ComponentModel.DataAnnotations;

namespace Funtime.Identity.Api.Models;

public class User
{
    [Key]
    public int Id { get; set; }

    [MaxLength(255)]
    public string? Email { get; set; }

    [MaxLength(255)]
    public string? PasswordHash { get; set; }

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// System role: "SU" for super admin, null for regular users
    /// </summary>
    [MaxLength(10)]
    public string? SystemRole { get; set; }

    public bool IsEmailVerified { get; set; } = false;

    public bool IsPhoneVerified { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    // Navigation properties
    public virtual ICollection<UserSite> UserSites { get; set; } = new List<UserSite>();
}

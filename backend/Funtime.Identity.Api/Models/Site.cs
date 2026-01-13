using System.ComponentModel.DataAnnotations;

namespace Funtime.Identity.Api.Models;

/// <summary>
/// Configuration for Funtime Pickleball sites
/// </summary>
public class Site
{
    [Key]
    [MaxLength(50)]
    public string Key { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(255)]
    public string? Url { get; set; }

    /// <summary>
    /// URL or path to the site's logo image
    /// </summary>
    [MaxLength(500)]
    public string? LogoUrl { get; set; }

    /// <summary>
    /// Whether the site is active and accessible
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether users need a subscription to access this site
    /// </summary>
    public bool RequiresSubscription { get; set; } = false;

    /// <summary>
    /// Monthly subscription price in cents (if applicable)
    /// </summary>
    public long? MonthlyPriceCents { get; set; }

    /// <summary>
    /// Yearly subscription price in cents (if applicable)
    /// </summary>
    public long? YearlyPriceCents { get; set; }

    /// <summary>
    /// Display order for site listings
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}

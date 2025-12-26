using System.ComponentModel.DataAnnotations;

namespace Funtime.Identity.Api.Models;

/// <summary>
/// Email/SMS notification template with Scriban syntax support
/// </summary>
public class NotificationTemplate
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Unique template code for programmatic reference (e.g., "welcome_email", "password_reset")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable template name
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Template type: Email, SMS, Push
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Type { get; set; } = "Email";

    /// <summary>
    /// Language code (e.g., "en", "es", "fr")
    /// </summary>
    [MaxLength(10)]
    public string Language { get; set; } = "en";

    /// <summary>
    /// Email subject line (supports Scriban templating)
    /// </summary>
    [MaxLength(500)]
    public string? Subject { get; set; }

    /// <summary>
    /// Template body - HTML for email, plain text for SMS (supports Scriban templating)
    /// </summary>
    [Required]
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Plain text version of email body (optional fallback)
    /// </summary>
    public string? BodyText { get; set; }

    /// <summary>
    /// Site key if template is site-specific (null for shared templates)
    /// </summary>
    [MaxLength(50)]
    public string? SiteKey { get; set; }

    /// <summary>
    /// Whether this template is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Optional description/notes about the template
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace Funtime.Identity.Api.Models;

/// <summary>
/// Notification task definition - configures how notifications are triggered and sent
/// </summary>
public class NotificationTask
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Unique task code for programmatic reference (e.g., "daily_digest", "order_confirmation")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable task name
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Task type: Email, SMS, Push, EmailAndSMS
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Type { get; set; } = "Email";

    /// <summary>
    /// Task status: Active, Testing, Inactive
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Active";

    /// <summary>
    /// Priority: Low, Normal, High, Critical
    /// </summary>
    [MaxLength(20)]
    public string Priority { get; set; } = "Normal";

    /// <summary>
    /// Mail profile ID for sending emails
    /// </summary>
    public int? MailProfileId { get; set; }

    /// <summary>
    /// Template ID to use for this task
    /// </summary>
    public int? TemplateId { get; set; }

    /// <summary>
    /// Site key if task is site-specific
    /// </summary>
    [MaxLength(50)]
    public string? SiteKey { get; set; }

    /// <summary>
    /// Default recipient list (comma-separated) for tasks without dynamic recipients
    /// </summary>
    [MaxLength(1000)]
    public string? DefaultRecipients { get; set; }

    /// <summary>
    /// CC recipients (comma-separated)
    /// </summary>
    [MaxLength(1000)]
    public string? CcRecipients { get; set; }

    /// <summary>
    /// BCC recipients (comma-separated)
    /// </summary>
    [MaxLength(1000)]
    public string? BccRecipients { get; set; }

    /// <summary>
    /// Test email address - when Status is Testing, send to this address instead
    /// </summary>
    [MaxLength(255)]
    public string? TestEmail { get; set; }

    /// <summary>
    /// Maximum retry attempts for failed sends
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Optional description/notes about the task
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public MailProfile? MailProfile { get; set; }
    public NotificationTemplate? Template { get; set; }
}

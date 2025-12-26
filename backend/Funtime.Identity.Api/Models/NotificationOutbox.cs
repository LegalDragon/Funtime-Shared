using System.ComponentModel.DataAnnotations;

namespace Funtime.Identity.Api.Models;

/// <summary>
/// Pending notification queue - messages waiting to be sent
/// </summary>
public class NotificationOutbox
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Task ID that generated this message
    /// </summary>
    public int? TaskId { get; set; }

    /// <summary>
    /// Notification type: Email, SMS, Push
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Type { get; set; } = "Email";

    /// <summary>
    /// Recipient email/phone (comma-separated for multiple)
    /// </summary>
    [Required]
    [MaxLength(1000)]
    public string ToList { get; set; } = string.Empty;

    /// <summary>
    /// CC recipients (email only)
    /// </summary>
    [MaxLength(1000)]
    public string? CcList { get; set; }

    /// <summary>
    /// BCC recipients (email only)
    /// </summary>
    [MaxLength(1000)]
    public string? BccList { get; set; }

    /// <summary>
    /// Sender email address
    /// </summary>
    [MaxLength(255)]
    public string? FromEmail { get; set; }

    /// <summary>
    /// Sender display name
    /// </summary>
    [MaxLength(100)]
    public string? FromName { get; set; }

    /// <summary>
    /// Email subject
    /// </summary>
    [MaxLength(500)]
    public string? Subject { get; set; }

    /// <summary>
    /// HTML body content
    /// </summary>
    public string? BodyHtml { get; set; }

    /// <summary>
    /// Plain text body content
    /// </summary>
    public string? BodyText { get; set; }

    /// <summary>
    /// JSON data for template rendering
    /// </summary>
    public string? TemplateData { get; set; }

    /// <summary>
    /// Status: Pending, Processing, Failed, Cancelled
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// Priority: Low, Normal, High, Critical
    /// </summary>
    [MaxLength(20)]
    public string Priority { get; set; } = "Normal";

    /// <summary>
    /// Number of send attempts
    /// </summary>
    public int Attempts { get; set; } = 0;

    /// <summary>
    /// Maximum retry attempts
    /// </summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// Last error message if failed
    /// </summary>
    [MaxLength(2000)]
    public string? LastError { get; set; }

    /// <summary>
    /// Scheduled send time (null = send immediately)
    /// </summary>
    public DateTime? ScheduledAt { get; set; }

    /// <summary>
    /// Next retry time
    /// </summary>
    public DateTime? NextRetryAt { get; set; }

    /// <summary>
    /// Site key for filtering
    /// </summary>
    [MaxLength(50)]
    public string? SiteKey { get; set; }

    /// <summary>
    /// Related user ID (if applicable)
    /// </summary>
    public int? UserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation property
    public NotificationTask? Task { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace Funtime.Identity.Api.Models;

/// <summary>
/// Notification history - record of sent messages
/// </summary>
public class NotificationHistory
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Original outbox ID
    /// </summary>
    public int? OutboxId { get; set; }

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
    /// Recipient email/phone
    /// </summary>
    [Required]
    [MaxLength(1000)]
    public string ToList { get; set; } = string.Empty;

    /// <summary>
    /// Sender email address
    /// </summary>
    [MaxLength(255)]
    public string? FromEmail { get; set; }

    /// <summary>
    /// Email subject
    /// </summary>
    [MaxLength(500)]
    public string? Subject { get; set; }

    /// <summary>
    /// Status: Sent, Delivered, Bounced, Failed
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Sent";

    /// <summary>
    /// Number of attempts before success
    /// </summary>
    public int Attempts { get; set; } = 1;

    /// <summary>
    /// External message ID (from SMTP server or SMS provider)
    /// </summary>
    [MaxLength(255)]
    public string? ExternalId { get; set; }

    /// <summary>
    /// Error message if failed
    /// </summary>
    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Site key for filtering
    /// </summary>
    [MaxLength(50)]
    public string? SiteKey { get; set; }

    /// <summary>
    /// Related user ID (if applicable)
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// When the message was sent
    /// </summary>
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the message was delivered (if tracked)
    /// </summary>
    public DateTime? DeliveredAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

using System.ComponentModel.DataAnnotations;

namespace Funtime.Identity.Api.Models;

/// <summary>
/// SMTP mail server configuration for sending notifications
/// </summary>
public class MailProfile
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Profile name for identification (e.g., "Primary SMTP", "Transactional")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// SMTP server hostname
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string SmtpHost { get; set; } = string.Empty;

    /// <summary>
    /// SMTP server port (typically 25, 465, or 587)
    /// </summary>
    public int SmtpPort { get; set; } = 587;

    /// <summary>
    /// Username for SMTP authentication
    /// </summary>
    [MaxLength(255)]
    public string? Username { get; set; }

    /// <summary>
    /// Password for SMTP authentication (encrypted)
    /// </summary>
    [MaxLength(500)]
    public string? Password { get; set; }

    /// <summary>
    /// Default sender email address
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>
    /// Default sender display name
    /// </summary>
    [MaxLength(100)]
    public string? FromName { get; set; }

    /// <summary>
    /// Security mode: None, SslOnConnect, StartTls, StartTlsWhenAvailable
    /// </summary>
    [MaxLength(30)]
    public string SecurityMode { get; set; } = "StartTlsWhenAvailable";

    /// <summary>
    /// Whether this profile is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Optional site key if profile is site-specific
    /// </summary>
    [MaxLength(50)]
    public string? SiteKey { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

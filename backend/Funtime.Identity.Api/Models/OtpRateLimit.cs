using System.ComponentModel.DataAnnotations;

namespace Funtime.Identity.Api.Models;

public class OtpRateLimit
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// The identifier (email or phone number) being rate limited
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string Identifier { get; set; } = string.Empty;

    public int RequestCount { get; set; } = 0;

    public DateTime WindowStart { get; set; } = DateTime.UtcNow;

    public DateTime? BlockedUntil { get; set; }
}

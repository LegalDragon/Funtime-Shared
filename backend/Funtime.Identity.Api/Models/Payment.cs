using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Funtime.Identity.Api.Models;

/// <summary>
/// Record of individual payments/transactions
/// </summary>
public class Payment
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PaymentCustomerId { get; set; }

    [ForeignKey("PaymentCustomerId")]
    public PaymentCustomer PaymentCustomer { get; set; } = null!;

    /// <summary>
    /// Stripe payment intent ID (pi_xxxxx) or charge ID (ch_xxxxx)
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string StripePaymentId { get; set; } = string.Empty;

    /// <summary>
    /// Amount in cents
    /// </summary>
    [Required]
    public long AmountCents { get; set; }

    /// <summary>
    /// Currency code (e.g., "usd")
    /// </summary>
    [MaxLength(3)]
    public string Currency { get; set; } = "usd";

    /// <summary>
    /// Payment status (e.g., "succeeded", "pending", "failed", "refunded")
    /// </summary>
    [MaxLength(50)]
    public string Status { get; set; } = "pending";

    /// <summary>
    /// Description of what the payment is for
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Which site this payment is for (e.g., "community", "college", "date", "jobs")
    /// </summary>
    [MaxLength(50)]
    public string? SiteKey { get; set; }

    /// <summary>
    /// Optional subscription ID if this payment is for a subscription
    /// </summary>
    public int? SubscriptionId { get; set; }

    [ForeignKey("SubscriptionId")]
    public Subscription? Subscription { get; set; }

    /// <summary>
    /// Additional metadata stored as JSON
    /// </summary>
    public string? Metadata { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}

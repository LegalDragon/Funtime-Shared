using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Funtime.Identity.Api.Models;

/// <summary>
/// Recurring subscription records
/// </summary>
public class Subscription
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PaymentCustomerId { get; set; }

    [ForeignKey("PaymentCustomerId")]
    public PaymentCustomer PaymentCustomer { get; set; } = null!;

    /// <summary>
    /// Stripe subscription ID (sub_xxxxx)
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string StripeSubscriptionId { get; set; } = string.Empty;

    /// <summary>
    /// Stripe price ID (price_xxxxx)
    /// </summary>
    [MaxLength(255)]
    public string? StripePriceId { get; set; }

    /// <summary>
    /// Stripe product ID (prod_xxxxx)
    /// </summary>
    [MaxLength(255)]
    public string? StripeProductId { get; set; }

    /// <summary>
    /// Subscription status (e.g., "active", "canceled", "past_due", "trialing")
    /// </summary>
    [MaxLength(50)]
    public string Status { get; set; } = "active";

    /// <summary>
    /// Plan name for display purposes
    /// </summary>
    [MaxLength(100)]
    public string? PlanName { get; set; }

    /// <summary>
    /// Which site this subscription is for (e.g., "community", "college", "date", "jobs")
    /// </summary>
    [MaxLength(50)]
    public string? SiteKey { get; set; }

    /// <summary>
    /// Amount per billing period in cents
    /// </summary>
    public long? AmountCents { get; set; }

    /// <summary>
    /// Currency code
    /// </summary>
    [MaxLength(3)]
    public string Currency { get; set; } = "usd";

    /// <summary>
    /// Billing interval (e.g., "month", "year")
    /// </summary>
    [MaxLength(20)]
    public string? Interval { get; set; }

    /// <summary>
    /// Current period start
    /// </summary>
    public DateTime? CurrentPeriodStart { get; set; }

    /// <summary>
    /// Current period end
    /// </summary>
    public DateTime? CurrentPeriodEnd { get; set; }

    /// <summary>
    /// When the subscription was canceled (if applicable)
    /// </summary>
    public DateTime? CanceledAt { get; set; }

    /// <summary>
    /// When the subscription will end (if set to cancel at period end)
    /// </summary>
    public DateTime? CancelAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

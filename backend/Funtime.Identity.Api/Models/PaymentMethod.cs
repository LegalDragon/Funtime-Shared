using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Funtime.Identity.Api.Models;

/// <summary>
/// Stored payment methods (cards) for a customer
/// </summary>
public class PaymentMethod
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PaymentCustomerId { get; set; }

    [ForeignKey("PaymentCustomerId")]
    public PaymentCustomer PaymentCustomer { get; set; } = null!;

    /// <summary>
    /// Stripe payment method ID (pm_xxxxx)
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string StripePaymentMethodId { get; set; } = string.Empty;

    /// <summary>
    /// Type of payment method (e.g., "card", "bank_account")
    /// </summary>
    [MaxLength(50)]
    public string Type { get; set; } = "card";

    /// <summary>
    /// Card brand (e.g., "visa", "mastercard", "amex")
    /// </summary>
    [MaxLength(50)]
    public string? CardBrand { get; set; }

    /// <summary>
    /// Last 4 digits of the card
    /// </summary>
    [MaxLength(4)]
    public string? CardLast4 { get; set; }

    /// <summary>
    /// Card expiration month
    /// </summary>
    public int? CardExpMonth { get; set; }

    /// <summary>
    /// Card expiration year
    /// </summary>
    public int? CardExpYear { get; set; }

    /// <summary>
    /// Whether this is the default payment method
    /// </summary>
    public bool IsDefault { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

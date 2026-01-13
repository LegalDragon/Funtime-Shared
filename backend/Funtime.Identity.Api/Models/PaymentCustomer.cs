using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Funtime.Identity.Api.Models;

/// <summary>
/// Links a user to their Stripe customer account
/// </summary>
public class PaymentCustomer
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [ForeignKey("UserId")]
    public User User { get; set; } = null!;

    /// <summary>
    /// Stripe customer ID (cus_xxxxx)
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string StripeCustomerId { get; set; } = string.Empty;

    /// <summary>
    /// Customer's email on file with Stripe
    /// </summary>
    [MaxLength(255)]
    public string? Email { get; set; }

    /// <summary>
    /// Customer's name on file with Stripe
    /// </summary>
    [MaxLength(255)]
    public string? Name { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<PaymentMethod> PaymentMethods { get; set; } = new List<PaymentMethod>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}

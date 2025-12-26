using System.ComponentModel.DataAnnotations;

namespace Funtime.Identity.Api.DTOs;

// Payment Customer DTOs

public class PaymentCustomerResponse
{
    public int Id { get; set; }
    public string StripeCustomerId { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<PaymentMethodResponse> PaymentMethods { get; set; } = new();
}

// Payment Method DTOs

public class PaymentMethodResponse
{
    public int Id { get; set; }
    public string StripePaymentMethodId { get; set; } = string.Empty;
    public string Type { get; set; } = "card";
    public string? CardBrand { get; set; }
    public string? CardLast4 { get; set; }
    public int? CardExpMonth { get; set; }
    public int? CardExpYear { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AttachPaymentMethodRequest
{
    [Required]
    public string StripePaymentMethodId { get; set; } = string.Empty;

    public bool SetAsDefault { get; set; } = false;
}

public class SetDefaultPaymentMethodRequest
{
    [Required]
    public string StripePaymentMethodId { get; set; } = string.Empty;
}

// Payment DTOs

public class PaymentResponse
{
    public int Id { get; set; }
    public string StripePaymentId { get; set; } = string.Empty;
    public long AmountCents { get; set; }
    public decimal AmountDollars => AmountCents / 100m;
    public string Currency { get; set; } = "usd";
    public string Status { get; set; } = "pending";
    public string? Description { get; set; }
    public string? SiteKey { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreatePaymentRequest
{
    [Required]
    [Range(50, 100000000)] // Min $0.50, max $1,000,000
    public long AmountCents { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(50)]
    public string? SiteKey { get; set; }
}

// Subscription DTOs

public class SubscriptionResponse
{
    public int Id { get; set; }
    public string StripeSubscriptionId { get; set; } = string.Empty;
    public string? StripePriceId { get; set; }
    public string Status { get; set; } = "active";
    public string? PlanName { get; set; }
    public string? SiteKey { get; set; }
    public long? AmountCents { get; set; }
    public decimal? AmountDollars => AmountCents.HasValue ? AmountCents.Value / 100m : null;
    public string Currency { get; set; } = "usd";
    public string? Interval { get; set; }
    public DateTime? CurrentPeriodStart { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }
    public DateTime? CanceledAt { get; set; }
    public DateTime? CancelAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateSubscriptionRequest
{
    [Required]
    public string StripePriceId { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? SiteKey { get; set; }
}

public class CancelSubscriptionRequest
{
    [Required]
    public int SubscriptionId { get; set; }

    public bool CancelAtPeriodEnd { get; set; } = true;
}

// Setup Intent for adding payment methods client-side
public class SetupIntentResponse
{
    public string ClientSecret { get; set; } = string.Empty;
}

// Payment Intent response for client-side confirmation
public class PaymentIntentResponse
{
    public string ClientSecret { get; set; } = string.Empty;
    public string PaymentIntentId { get; set; } = string.Empty;
    public long AmountCents { get; set; }
    public string Status { get; set; } = string.Empty;
}

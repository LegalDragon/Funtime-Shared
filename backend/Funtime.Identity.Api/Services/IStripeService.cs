using Funtime.Identity.Api.Models;

namespace Funtime.Identity.Api.Services;

public interface IStripeService
{
    // Customer operations
    Task<PaymentCustomer> GetOrCreateCustomerAsync(int userId, string? email = null, string? name = null);
    Task<PaymentCustomer?> GetCustomerByUserIdAsync(int userId);

    // Payment method operations
    Task<PaymentMethod> AttachPaymentMethodAsync(int userId, string stripePaymentMethodId, bool setAsDefault = false);
    Task<List<PaymentMethod>> GetPaymentMethodsAsync(int userId);
    Task<bool> DetachPaymentMethodAsync(int userId, string stripePaymentMethodId);
    Task<bool> SetDefaultPaymentMethodAsync(int userId, string stripePaymentMethodId);

    // Payment operations
    Task<Payment> CreatePaymentIntentAsync(int userId, long amountCents, string? description = null, string? siteKey = null);
    Task<Payment?> ConfirmPaymentAsync(string stripePaymentIntentId);
    Task<List<Payment>> GetPaymentHistoryAsync(int userId, int limit = 20);

    // Subscription operations
    Task<Subscription> CreateSubscriptionAsync(int userId, string stripePriceId, string? siteKey = null);
    Task<Subscription?> GetSubscriptionAsync(int userId, string? siteKey = null);
    Task<List<Subscription>> GetSubscriptionsAsync(int userId);
    Task<Subscription?> CancelSubscriptionAsync(int userId, int subscriptionId, bool cancelAtPeriodEnd = true);

    // Webhook handling
    Task HandleWebhookEventAsync(string json, string stripeSignature);
}

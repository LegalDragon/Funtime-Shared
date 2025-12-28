using Microsoft.EntityFrameworkCore;
using Stripe;
using Funtime.Identity.Api.Data;
using Funtime.Identity.Api.Models;

namespace Funtime.Identity.Api.Services;

public class StripeService : IStripeService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripeService> _logger;

    public StripeService(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<StripeService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;

        var secretKey = _configuration["Stripe:SecretKey"];
        if (!string.IsNullOrEmpty(secretKey) && secretKey != "YOUR_STRIPE_SECRET_KEY")
        {
            StripeConfiguration.ApiKey = secretKey;
        }
    }

    public async Task<PaymentCustomer> GetOrCreateCustomerAsync(int userId, string? email = null, string? name = null)
    {
        var existing = await _context.PaymentCustomers
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (existing != null)
        {
            return existing;
        }

        // Get user info
        var user = await _context.Users.FindAsync(userId);
        var userEmail = email ?? user?.Email;
        var userName = name;

        // Create Stripe customer
        var customerService = new CustomerService();
        var stripeCustomer = await customerService.CreateAsync(new CustomerCreateOptions
        {
            Email = userEmail,
            Name = userName,
            Metadata = new Dictionary<string, string>
            {
                { "funtime_user_id", userId.ToString() }
            }
        });

        var paymentCustomer = new PaymentCustomer
        {
            UserId = userId,
            StripeCustomerId = stripeCustomer.Id,
            Email = userEmail,
            Name = userName,
            CreatedAt = DateTime.UtcNow
        };

        _context.PaymentCustomers.Add(paymentCustomer);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created Stripe customer {StripeCustomerId} for user {UserId}", stripeCustomer.Id, userId);

        return paymentCustomer;
    }

    public async Task<PaymentCustomer?> GetCustomerByUserIdAsync(int userId)
    {
        return await _context.PaymentCustomers
            .Include(c => c.PaymentMethods)
            .Include(c => c.Subscriptions)
            .FirstOrDefaultAsync(c => c.UserId == userId);
    }

    public async Task<Models.PaymentMethod> AttachPaymentMethodAsync(int userId, string stripePaymentMethodId, bool setAsDefault = false)
    {
        var customer = await GetOrCreateCustomerAsync(userId);

        // Attach payment method to customer in Stripe
        var paymentMethodService = new PaymentMethodService();
        var stripePaymentMethod = await paymentMethodService.AttachAsync(stripePaymentMethodId, new PaymentMethodAttachOptions
        {
            Customer = customer.StripeCustomerId
        });

        // If setting as default, update in Stripe
        if (setAsDefault)
        {
            var customerService = new CustomerService();
            await customerService.UpdateAsync(customer.StripeCustomerId, new CustomerUpdateOptions
            {
                InvoiceSettings = new CustomerInvoiceSettingsOptions
                {
                    DefaultPaymentMethod = stripePaymentMethodId
                }
            });

            // Mark all existing as non-default
            var existingMethods = await _context.PaymentMethods
                .Where(m => m.PaymentCustomerId == customer.Id)
                .ToListAsync();
            foreach (var m in existingMethods)
            {
                m.IsDefault = false;
            }
        }

        var paymentMethod = new Models.PaymentMethod
        {
            PaymentCustomerId = customer.Id,
            StripePaymentMethodId = stripePaymentMethodId,
            Type = stripePaymentMethod.Type,
            CardBrand = stripePaymentMethod.Card?.Brand,
            CardLast4 = stripePaymentMethod.Card?.Last4,
            CardExpMonth = (int?)stripePaymentMethod.Card?.ExpMonth,
            CardExpYear = (int?)stripePaymentMethod.Card?.ExpYear,
            IsDefault = setAsDefault,
            CreatedAt = DateTime.UtcNow
        };

        _context.PaymentMethods.Add(paymentMethod);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Attached payment method {PaymentMethodId} for user {UserId}", stripePaymentMethodId, userId);

        return paymentMethod;
    }

    public async Task<List<Models.PaymentMethod>> GetPaymentMethodsAsync(int userId)
    {
        var customer = await _context.PaymentCustomers
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (customer == null)
        {
            return new List<Models.PaymentMethod>();
        }

        return await _context.PaymentMethods
            .Where(m => m.PaymentCustomerId == customer.Id)
            .OrderByDescending(m => m.IsDefault)
            .ThenByDescending(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> DetachPaymentMethodAsync(int userId, string stripePaymentMethodId)
    {
        var customer = await _context.PaymentCustomers
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (customer == null)
        {
            return false;
        }

        var paymentMethod = await _context.PaymentMethods
            .FirstOrDefaultAsync(m => m.PaymentCustomerId == customer.Id && m.StripePaymentMethodId == stripePaymentMethodId);

        if (paymentMethod == null)
        {
            return false;
        }

        // Detach from Stripe
        var paymentMethodService = new PaymentMethodService();
        await paymentMethodService.DetachAsync(stripePaymentMethodId);

        // Remove from database
        _context.PaymentMethods.Remove(paymentMethod);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Detached payment method {PaymentMethodId} for user {UserId}", stripePaymentMethodId, userId);

        return true;
    }

    public async Task<bool> SetDefaultPaymentMethodAsync(int userId, string stripePaymentMethodId)
    {
        var customer = await _context.PaymentCustomers
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (customer == null)
        {
            return false;
        }

        // Update in Stripe
        var customerService = new CustomerService();
        await customerService.UpdateAsync(customer.StripeCustomerId, new CustomerUpdateOptions
        {
            InvoiceSettings = new CustomerInvoiceSettingsOptions
            {
                DefaultPaymentMethod = stripePaymentMethodId
            }
        });

        // Update in database
        var allMethods = await _context.PaymentMethods
            .Where(m => m.PaymentCustomerId == customer.Id)
            .ToListAsync();

        foreach (var method in allMethods)
        {
            method.IsDefault = method.StripePaymentMethodId == stripePaymentMethodId;
        }

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<Models.Payment> CreatePaymentIntentAsync(int userId, long amountCents, string? description = null, string? siteKey = null)
    {
        var customer = await GetOrCreateCustomerAsync(userId);

        var paymentIntentService = new PaymentIntentService();
        var paymentIntent = await paymentIntentService.CreateAsync(new PaymentIntentCreateOptions
        {
            Amount = amountCents,
            Currency = "usd",
            Customer = customer.StripeCustomerId,
            Description = description,
            Metadata = new Dictionary<string, string>
            {
                { "funtime_user_id", userId.ToString() },
                { "site_key", siteKey ?? "" }
            }
        });

        var payment = new Models.Payment
        {
            PaymentCustomerId = customer.Id,
            StripePaymentId = paymentIntent.Id,
            AmountCents = amountCents,
            Currency = "usd",
            Status = paymentIntent.Status,
            Description = description,
            SiteKey = siteKey,
            CreatedAt = DateTime.UtcNow
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created payment intent {PaymentIntentId} for user {UserId}, amount {Amount}", paymentIntent.Id, userId, amountCents);

        return payment;
    }

    public async Task<Models.Payment?> ConfirmPaymentAsync(string stripePaymentIntentId)
    {
        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.StripePaymentId == stripePaymentIntentId);

        if (payment == null)
        {
            return null;
        }

        var paymentIntentService = new PaymentIntentService();
        var paymentIntent = await paymentIntentService.GetAsync(stripePaymentIntentId);

        payment.Status = paymentIntent.Status;
        payment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return payment;
    }

    public async Task<List<Models.Payment>> GetPaymentHistoryAsync(int userId, int limit = 20)
    {
        var customer = await _context.PaymentCustomers
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (customer == null)
        {
            return new List<Models.Payment>();
        }

        return await _context.Payments
            .Where(p => p.PaymentCustomerId == customer.Id)
            .OrderByDescending(p => p.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<Models.Subscription> CreateSubscriptionAsync(int userId, string stripePriceId, string? siteKey = null)
    {
        var customer = await GetOrCreateCustomerAsync(userId);

        var subscriptionService = new SubscriptionService();
        var stripeSubscription = await subscriptionService.CreateAsync(new SubscriptionCreateOptions
        {
            Customer = customer.StripeCustomerId,
            Items = new List<SubscriptionItemOptions>
            {
                new SubscriptionItemOptions { Price = stripePriceId }
            },
            Metadata = new Dictionary<string, string>
            {
                { "funtime_user_id", userId.ToString() },
                { "site_key", siteKey ?? "" }
            }
        });

        var subscription = new Models.Subscription
        {
            PaymentCustomerId = customer.Id,
            StripeSubscriptionId = stripeSubscription.Id,
            StripePriceId = stripePriceId,
            Status = stripeSubscription.Status,
            SiteKey = siteKey,
            CurrentPeriodStart = stripeSubscription.CurrentPeriodStart,
            CurrentPeriodEnd = stripeSubscription.CurrentPeriodEnd,
            CreatedAt = DateTime.UtcNow
        };

        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created subscription {SubscriptionId} for user {UserId}", stripeSubscription.Id, userId);

        return subscription;
    }

    public async Task<Models.Subscription?> GetSubscriptionAsync(int userId, string? siteKey = null)
    {
        var customer = await _context.PaymentCustomers
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (customer == null)
        {
            return null;
        }

        var query = _context.Subscriptions
            .Where(s => s.PaymentCustomerId == customer.Id && s.Status == "active");

        if (!string.IsNullOrEmpty(siteKey))
        {
            query = query.Where(s => s.SiteKey == siteKey);
        }

        return await query.FirstOrDefaultAsync();
    }

    public async Task<List<Models.Subscription>> GetSubscriptionsAsync(int userId)
    {
        var customer = await _context.PaymentCustomers
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (customer == null)
        {
            return new List<Models.Subscription>();
        }

        return await _context.Subscriptions
            .Where(s => s.PaymentCustomerId == customer.Id)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<Models.Subscription?> CancelSubscriptionAsync(int userId, int subscriptionId, bool cancelAtPeriodEnd = true)
    {
        var subscription = await _context.Subscriptions
            .Include(s => s.PaymentCustomer)
            .FirstOrDefaultAsync(s => s.Id == subscriptionId && s.PaymentCustomer.UserId == userId);

        if (subscription == null)
        {
            return null;
        }

        var subscriptionService = new SubscriptionService();

        if (cancelAtPeriodEnd)
        {
            await subscriptionService.UpdateAsync(subscription.StripeSubscriptionId, new SubscriptionUpdateOptions
            {
                CancelAtPeriodEnd = true
            });
            subscription.CancelAt = subscription.CurrentPeriodEnd;
        }
        else
        {
            await subscriptionService.CancelAsync(subscription.StripeSubscriptionId);
            subscription.Status = "canceled";
            subscription.CanceledAt = DateTime.UtcNow;
        }

        subscription.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Canceled subscription {SubscriptionId} for user {UserId}", subscription.StripeSubscriptionId, userId);

        return subscription;
    }

    public async Task HandleWebhookEventAsync(string json, string stripeSignature)
    {
        var webhookSecret = _configuration["Stripe:WebhookSecret"];

        if (string.IsNullOrEmpty(webhookSecret) || webhookSecret == "YOUR_STRIPE_WEBHOOK_SECRET")
        {
            _logger.LogWarning("Stripe webhook secret not configured");
            return;
        }

        var stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, webhookSecret);

        switch (stripeEvent.Type)
        {
            case "payment_intent.succeeded":
                var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                if (paymentIntent != null)
                {
                    await ConfirmPaymentAsync(paymentIntent.Id);
                }
                break;

            case "customer.subscription.updated":
            case "customer.subscription.deleted":
                var subscription = stripeEvent.Data.Object as Stripe.Subscription;
                if (subscription != null)
                {
                    await UpdateSubscriptionFromStripeAsync(subscription);
                }
                break;

            case "invoice.paid":
                var invoice = stripeEvent.Data.Object as Invoice;
                _logger.LogInformation("Invoice paid: {InvoiceId}", invoice?.Id);
                break;

            default:
                _logger.LogInformation("Unhandled Stripe event type: {EventType}", stripeEvent.Type);
                break;
        }
    }

    private async Task UpdateSubscriptionFromStripeAsync(Stripe.Subscription stripeSubscription)
    {
        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscription.Id);

        if (subscription == null)
        {
            return;
        }

        subscription.Status = stripeSubscription.Status;
        subscription.CurrentPeriodStart = stripeSubscription.CurrentPeriodStart;
        subscription.CurrentPeriodEnd = stripeSubscription.CurrentPeriodEnd;
        subscription.CanceledAt = stripeSubscription.CanceledAt;
        subscription.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }
}

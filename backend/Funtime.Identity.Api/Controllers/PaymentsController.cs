using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Funtime.Identity.Api.DTOs;
using Funtime.Identity.Api.Services;

namespace Funtime.Identity.Api.Controllers;

[ApiController]
[Route("payments")]
public class PaymentsController : ControllerBase
{
    private readonly IStripeService _stripeService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IStripeService stripeService,
        IConfiguration configuration,
        ILogger<PaymentsController> logger)
    {
        _stripeService = stripeService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Get or create payment customer for current user
    /// </summary>
    [Authorize]
    [HttpGet("customer")]
    public async Task<ActionResult<PaymentCustomerResponse>> GetCustomer()
    {
        var userId = GetUserIdFromToken();
        if (userId == null)
        {
            return Unauthorized(new ApiResponse { Success = false, Message = "Invalid token." });
        }

        var customer = await _stripeService.GetOrCreateCustomerAsync(userId.Value);

        return Ok(new PaymentCustomerResponse
        {
            Id = customer.Id,
            StripeCustomerId = customer.StripeCustomerId,
            Email = customer.Email,
            Name = customer.Name,
            CreatedAt = customer.CreatedAt
        });
    }

    /// <summary>
    /// Create a setup intent for adding a new payment method
    /// </summary>
    [Authorize]
    [HttpPost("setup-intent")]
    public async Task<ActionResult<SetupIntentResponse>> CreateSetupIntent()
    {
        var userId = GetUserIdFromToken();
        if (userId == null)
        {
            return Unauthorized(new ApiResponse { Success = false, Message = "Invalid token." });
        }

        var customer = await _stripeService.GetOrCreateCustomerAsync(userId.Value);

        var setupIntentService = new SetupIntentService();
        var setupIntent = await setupIntentService.CreateAsync(new SetupIntentCreateOptions
        {
            Customer = customer.StripeCustomerId,
            PaymentMethodTypes = new List<string> { "card" }
        });

        return Ok(new SetupIntentResponse
        {
            ClientSecret = setupIntent.ClientSecret
        });
    }

    /// <summary>
    /// Attach a payment method to the current user
    /// </summary>
    [Authorize]
    [HttpPost("payment-methods")]
    public async Task<ActionResult<PaymentMethodResponse>> AttachPaymentMethod([FromBody] AttachPaymentMethodRequest request)
    {
        var userId = GetUserIdFromToken();
        if (userId == null)
        {
            return Unauthorized(new ApiResponse { Success = false, Message = "Invalid token." });
        }

        try
        {
            var paymentMethod = await _stripeService.AttachPaymentMethodAsync(
                userId.Value,
                request.StripePaymentMethodId,
                request.SetAsDefault);

            return Ok(new PaymentMethodResponse
            {
                Id = paymentMethod.Id,
                StripePaymentMethodId = paymentMethod.StripePaymentMethodId,
                Type = paymentMethod.Type,
                CardBrand = paymentMethod.CardBrand,
                CardLast4 = paymentMethod.CardLast4,
                CardExpMonth = paymentMethod.CardExpMonth,
                CardExpYear = paymentMethod.CardExpYear,
                IsDefault = paymentMethod.IsDefault,
                CreatedAt = paymentMethod.CreatedAt
            });
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to attach payment method");
            return BadRequest(new ApiResponse { Success = false, Message = ex.Message });
        }
    }

    /// <summary>
    /// Get all payment methods for current user
    /// </summary>
    [Authorize]
    [HttpGet("payment-methods")]
    public async Task<ActionResult<List<PaymentMethodResponse>>> GetPaymentMethods()
    {
        var userId = GetUserIdFromToken();
        if (userId == null)
        {
            return Unauthorized(new ApiResponse { Success = false, Message = "Invalid token." });
        }

        var methods = await _stripeService.GetPaymentMethodsAsync(userId.Value);

        return Ok(methods.Select(m => new PaymentMethodResponse
        {
            Id = m.Id,
            StripePaymentMethodId = m.StripePaymentMethodId,
            Type = m.Type,
            CardBrand = m.CardBrand,
            CardLast4 = m.CardLast4,
            CardExpMonth = m.CardExpMonth,
            CardExpYear = m.CardExpYear,
            IsDefault = m.IsDefault,
            CreatedAt = m.CreatedAt
        }).ToList());
    }

    /// <summary>
    /// Set default payment method
    /// </summary>
    [Authorize]
    [HttpPost("payment-methods/default")]
    public async Task<ActionResult<ApiResponse>> SetDefaultPaymentMethod([FromBody] SetDefaultPaymentMethodRequest request)
    {
        var userId = GetUserIdFromToken();
        if (userId == null)
        {
            return Unauthorized(new ApiResponse { Success = false, Message = "Invalid token." });
        }

        var success = await _stripeService.SetDefaultPaymentMethodAsync(userId.Value, request.StripePaymentMethodId);

        if (!success)
        {
            return NotFound(new ApiResponse { Success = false, Message = "Payment method not found." });
        }

        return Ok(new ApiResponse { Success = true, Message = "Default payment method updated." });
    }

    /// <summary>
    /// Delete a payment method
    /// </summary>
    [Authorize]
    [HttpDelete("payment-methods/{stripePaymentMethodId}")]
    public async Task<ActionResult<ApiResponse>> DeletePaymentMethod(string stripePaymentMethodId)
    {
        var userId = GetUserIdFromToken();
        if (userId == null)
        {
            return Unauthorized(new ApiResponse { Success = false, Message = "Invalid token." });
        }

        var success = await _stripeService.DetachPaymentMethodAsync(userId.Value, stripePaymentMethodId);

        if (!success)
        {
            return NotFound(new ApiResponse { Success = false, Message = "Payment method not found." });
        }

        return Ok(new ApiResponse { Success = true, Message = "Payment method deleted." });
    }

    /// <summary>
    /// Create a payment intent for one-time payment
    /// </summary>
    [Authorize]
    [HttpPost("create-payment")]
    public async Task<ActionResult<PaymentIntentResponse>> CreatePayment([FromBody] CreatePaymentRequest request)
    {
        var userId = GetUserIdFromToken();
        if (userId == null)
        {
            return Unauthorized(new ApiResponse { Success = false, Message = "Invalid token." });
        }

        try
        {
            var payment = await _stripeService.CreatePaymentIntentAsync(
                userId.Value,
                request.AmountCents,
                request.Description,
                request.SiteKey);

            // Get the client secret from Stripe
            var paymentIntentService = new PaymentIntentService();
            var paymentIntent = await paymentIntentService.GetAsync(payment.StripePaymentId);

            return Ok(new PaymentIntentResponse
            {
                ClientSecret = paymentIntent.ClientSecret,
                PaymentIntentId = payment.StripePaymentId,
                AmountCents = payment.AmountCents,
                Status = payment.Status
            });
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to create payment");
            return BadRequest(new ApiResponse { Success = false, Message = ex.Message });
        }
    }

    /// <summary>
    /// Get payment history for current user
    /// </summary>
    [Authorize]
    [HttpGet("history")]
    public async Task<ActionResult<List<PaymentResponse>>> GetPaymentHistory([FromQuery] int limit = 20)
    {
        var userId = GetUserIdFromToken();
        if (userId == null)
        {
            return Unauthorized(new ApiResponse { Success = false, Message = "Invalid token." });
        }

        var payments = await _stripeService.GetPaymentHistoryAsync(userId.Value, limit);

        return Ok(payments.Select(p => new PaymentResponse
        {
            Id = p.Id,
            StripePaymentId = p.StripePaymentId,
            AmountCents = p.AmountCents,
            Currency = p.Currency,
            Status = p.Status,
            Description = p.Description,
            SiteKey = p.SiteKey,
            CreatedAt = p.CreatedAt
        }).ToList());
    }

    /// <summary>
    /// Create a subscription
    /// </summary>
    [Authorize]
    [HttpPost("subscriptions")]
    public async Task<ActionResult<SubscriptionResponse>> CreateSubscription([FromBody] CreateSubscriptionRequest request)
    {
        var userId = GetUserIdFromToken();
        if (userId == null)
        {
            return Unauthorized(new ApiResponse { Success = false, Message = "Invalid token." });
        }

        try
        {
            var subscription = await _stripeService.CreateSubscriptionAsync(
                userId.Value,
                request.StripePriceId,
                request.SiteKey);

            return Ok(MapToSubscriptionResponse(subscription));
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to create subscription");
            return BadRequest(new ApiResponse { Success = false, Message = ex.Message });
        }
    }

    /// <summary>
    /// Get all subscriptions for current user
    /// </summary>
    [Authorize]
    [HttpGet("subscriptions")]
    public async Task<ActionResult<List<SubscriptionResponse>>> GetSubscriptions()
    {
        var userId = GetUserIdFromToken();
        if (userId == null)
        {
            return Unauthorized(new ApiResponse { Success = false, Message = "Invalid token." });
        }

        var subscriptions = await _stripeService.GetSubscriptionsAsync(userId.Value);

        return Ok(subscriptions.Select(MapToSubscriptionResponse).ToList());
    }

    /// <summary>
    /// Cancel a subscription
    /// </summary>
    [Authorize]
    [HttpPost("subscriptions/cancel")]
    public async Task<ActionResult<SubscriptionResponse>> CancelSubscription([FromBody] CancelSubscriptionRequest request)
    {
        var userId = GetUserIdFromToken();
        if (userId == null)
        {
            return Unauthorized(new ApiResponse { Success = false, Message = "Invalid token." });
        }

        var subscription = await _stripeService.CancelSubscriptionAsync(
            userId.Value,
            request.SubscriptionId,
            request.CancelAtPeriodEnd);

        if (subscription == null)
        {
            return NotFound(new ApiResponse { Success = false, Message = "Subscription not found." });
        }

        return Ok(MapToSubscriptionResponse(subscription));
    }

    /// <summary>
    /// Stripe webhook endpoint
    /// </summary>
    [HttpPost("webhook")]
    public async Task<IActionResult> StripeWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var signature = Request.Headers["Stripe-Signature"].FirstOrDefault();

        if (string.IsNullOrEmpty(signature))
        {
            return BadRequest("Missing Stripe-Signature header");
        }

        try
        {
            await _stripeService.HandleWebhookEventAsync(json, signature);
            return Ok();
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe webhook error");
            return BadRequest(ex.Message);
        }
    }

    private int? GetUserIdFromToken()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private static SubscriptionResponse MapToSubscriptionResponse(Models.Subscription s)
    {
        return new SubscriptionResponse
        {
            Id = s.Id,
            StripeSubscriptionId = s.StripeSubscriptionId,
            StripePriceId = s.StripePriceId,
            Status = s.Status,
            PlanName = s.PlanName,
            SiteKey = s.SiteKey,
            AmountCents = s.AmountCents,
            Currency = s.Currency,
            Interval = s.Interval,
            CurrentPeriodStart = s.CurrentPeriodStart,
            CurrentPeriodEnd = s.CurrentPeriodEnd,
            CanceledAt = s.CanceledAt,
            CancelAt = s.CancelAt,
            CreatedAt = s.CreatedAt
        };
    }
}

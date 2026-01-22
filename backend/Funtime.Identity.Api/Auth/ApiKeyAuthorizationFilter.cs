using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Funtime.Identity.Api.Services;
using Funtime.Identity.Api.Models;

namespace Funtime.Identity.Api.Auth;

/// <summary>
/// Attribute to require API key authentication with optional scope requirement.
/// Supports dual auth mode where either API key OR JWT is accepted.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class ApiKeyAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string? _requiredScope;

    /// <summary>
    /// When true, allows JWT authentication as an alternative to API key.
    /// If both are provided, API key takes precedence.
    /// </summary>
    public bool AllowJwt { get; set; } = false;

    /// <summary>
    /// Require API key authentication
    /// </summary>
    public ApiKeyAuthorizeAttribute()
    {
    }

    /// <summary>
    /// Require API key authentication with specific scope
    /// </summary>
    /// <param name="requiredScope">The scope required (e.g., "assets:write")</param>
    public ApiKeyAuthorizeAttribute(string requiredScope)
    {
        _requiredScope = requiredScope;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var apiKeyService = context.HttpContext.RequestServices.GetRequiredService<IApiKeyService>();
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<ApiKeyAuthorizeAttribute>>();

        // Get API key from header
        var apiKey = context.HttpContext.Request.Headers["X-Api-Key"].FirstOrDefault();

        // If no API key provided, check if JWT fallback is allowed
        if (string.IsNullOrEmpty(apiKey))
        {
            if (AllowJwt && context.HttpContext.User.Identity?.IsAuthenticated == true)
            {
                // JWT is valid, allow the request
                return;
            }

            var message = AllowJwt
                ? "Authentication required. Provide X-Api-Key header or valid JWT Bearer token."
                : "API key is required. Include X-Api-Key header.";
            context.Result = new UnauthorizedObjectResult(new { message });
            return;
        }

        // Validate API key
        var key = await apiKeyService.ValidateKeyAsync(apiKey);

        if (key == null)
        {
            logger.LogWarning("Invalid API key attempt: {KeyPrefix}...", apiKey.Length > 10 ? apiKey[..10] : apiKey);
            context.Result = new UnauthorizedObjectResult(new { message = "Invalid or expired API key." });
            return;
        }

        // Check IP restriction if configured
        if (key.AllowedIPsList.Count > 0)
        {
            var clientIp = context.HttpContext.Connection.RemoteIpAddress?.ToString();
            if (clientIp != null && !IsIpAllowed(clientIp, key.AllowedIPsList))
            {
                logger.LogWarning("API key {PartnerKey} used from unauthorized IP: {IP}", key.PartnerKey, clientIp);
                context.Result = new ObjectResult(new { message = "Access denied from this IP address." })
                {
                    StatusCode = 403
                };
                return;
            }
        }

        // Check scope if required
        if (!string.IsNullOrEmpty(_requiredScope) && !key.HasScope(_requiredScope))
        {
            logger.LogWarning("API key {PartnerKey} lacks required scope: {Scope}", key.PartnerKey, _requiredScope);
            context.Result = new ObjectResult(new { message = $"This API key does not have the required scope: {_requiredScope}" })
            {
                StatusCode = 403
            };
            return;
        }

        // Store API key info in HttpContext for later use
        context.HttpContext.Items["ApiKey"] = key;
        context.HttpContext.Items["ApiKeyPartner"] = key.PartnerKey;

        // Record usage (fire and forget)
        _ = apiKeyService.RecordUsageAsync(apiKey);
    }

    private static bool IsIpAllowed(string clientIp, List<string> allowedIPs)
    {
        foreach (var allowed in allowedIPs)
        {
            // Simple exact match for now
            // TODO: Add CIDR range support
            if (allowed == clientIp || allowed == "*")
            {
                return true;
            }
        }
        return false;
    }
}

/// <summary>
/// Extension methods for accessing API key info from HttpContext
/// </summary>
public static class ApiKeyHttpContextExtensions
{
    /// <summary>
    /// Get the authenticated API key from the request
    /// </summary>
    public static ApiKey? GetApiKey(this HttpContext context)
    {
        return context.Items.TryGetValue("ApiKey", out var key) ? key as ApiKey : null;
    }

    /// <summary>
    /// Get the partner key from the authenticated API key
    /// </summary>
    public static string? GetApiKeyPartner(this HttpContext context)
    {
        return context.Items.TryGetValue("ApiKeyPartner", out var partner) ? partner as string : null;
    }

    /// <summary>
    /// Check if the request is authenticated via API key
    /// </summary>
    public static bool IsApiKeyAuthenticated(this HttpContext context)
    {
        return context.Items.ContainsKey("ApiKey");
    }
}

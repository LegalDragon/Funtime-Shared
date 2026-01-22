using Microsoft.AspNetCore.Mvc;
using Funtime.Identity.Api.Auth;
using Funtime.Identity.Api.Models;
using Funtime.Identity.Api.Services;

namespace Funtime.Identity.Api.Controllers;

/// <summary>
/// Public endpoint for partners to test their API key integration
/// </summary>
[ApiController]
[Route("apikey")]
public class ApiKeyTestController : ControllerBase
{
    private readonly IApiKeyService _apiKeyService;
    private readonly ILogger<ApiKeyTestController> _logger;

    public ApiKeyTestController(
        IApiKeyService apiKeyService,
        ILogger<ApiKeyTestController> logger)
    {
        _apiKeyService = apiKeyService;
        _logger = logger;
    }

    /// <summary>
    /// Test your API key and see its configuration
    /// Returns partner info, granted scopes, and rate limit details
    /// </summary>
    [HttpGet("test")]
    [ApiKeyAuthorize]  // Any valid API key works
    public async Task<ActionResult<ApiKeyTestResponse>> TestApiKey()
    {
        var apiKey = HttpContext.Request.Headers["X-Api-Key"].FirstOrDefault();
        if (string.IsNullOrEmpty(apiKey))
        {
            return Unauthorized(new { message = "X-Api-Key header is required." });
        }

        var keyInfo = await _apiKeyService.ValidateKeyAsync(apiKey);
        if (keyInfo == null)
        {
            return Unauthorized(new { message = "Invalid API key." });
        }

        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        _logger.LogInformation("API key test from partner {Partner} at IP {Ip}",
            keyInfo.PartnerKey, clientIp);

        return Ok(new ApiKeyTestResponse
        {
            Success = true,
            Message = "API key is valid and working!",
            PartnerKey = keyInfo.PartnerKey,
            PartnerName = keyInfo.PartnerName,
            Scopes = keyInfo.ScopesList,
            RateLimitPerMinute = keyInfo.RateLimitPerMinute,
            IsActive = keyInfo.IsActive,
            ClientIp = clientIp,
            AllowedIps = keyInfo.AllowedIPsList.Count > 0 ? keyInfo.AllowedIPsList : null,
            ExpiresAt = keyInfo.ExpiresAt,
            LastUsedAt = keyInfo.LastUsedAt,
            ServerTime = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Test a specific scope - returns whether your API key has the requested scope
    /// </summary>
    [HttpGet("test/scope/{scope}")]
    [ApiKeyAuthorize]
    public async Task<ActionResult<ScopeTestResponse>> TestScope(string scope)
    {
        var apiKey = HttpContext.Request.Headers["X-Api-Key"].FirstOrDefault();
        if (string.IsNullOrEmpty(apiKey))
        {
            return Unauthorized(new { message = "X-Api-Key header is required." });
        }

        var keyInfo = await _apiKeyService.ValidateKeyAsync(apiKey);
        if (keyInfo == null)
        {
            return Unauthorized(new { message = "Invalid API key." });
        }

        var hasScope = keyInfo.HasScope(scope);
        var validScope = ApiScopes.AllScopes.Contains(scope);
        var keyScopes = keyInfo.ScopesList;

        return Ok(new ScopeTestResponse
        {
            Scope = scope,
            IsValidScope = validScope,
            HasScope = hasScope,
            Message = !validScope
                ? $"'{scope}' is not a valid scope. Valid scopes: {string.Join(", ", ApiScopes.AllScopes)}"
                : hasScope
                    ? $"Your API key has the '{scope}' scope."
                    : $"Your API key does NOT have the '{scope}' scope. Your scopes: {string.Join(", ", keyScopes)}"
        });
    }

    /// <summary>
    /// Get list of all available scopes (no auth required)
    /// </summary>
    [HttpGet("scopes")]
    public ActionResult<AvailableScopesResponse> GetAvailableScopes()
    {
        return Ok(new AvailableScopesResponse
        {
            Scopes = ApiScopes.AllScopes.Select(s => new ScopeInfo
            {
                Name = s,
                Description = GetScopeDescription(s)
            }).ToList()
        });
    }

    private static string GetScopeDescription(string scope) => scope switch
    {
        ApiScopes.AuthValidate => "Validate JWT tokens",
        ApiScopes.AuthSync => "Sync user authentication (force-auth, external login)",
        ApiScopes.UsersRead => "Read user profiles and information",
        ApiScopes.UsersWrite => "Update user information (email, password, roles)",
        ApiScopes.AssetsRead => "Read/download assets",
        ApiScopes.AssetsWrite => "Upload, link, and delete assets",
        ApiScopes.SitesRead => "Read site membership information",
        ApiScopes.PushSend => "Send push notifications via SignalR",
        ApiScopes.Admin => "Full administrative access (includes all scopes)",
        _ => "Unknown scope"
    };
}

#region DTOs

public class ApiKeyTestResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string PartnerKey { get; set; } = string.Empty;
    public string PartnerName { get; set; } = string.Empty;
    public List<string> Scopes { get; set; } = new();
    public int RateLimitPerMinute { get; set; }
    public int CurrentRequestsInWindow { get; set; }
    public bool IsActive { get; set; }
    public string ClientIp { get; set; } = string.Empty;
    public List<string>? AllowedIps { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime ServerTime { get; set; }
}

public class ScopeTestResponse
{
    public string Scope { get; set; } = string.Empty;
    public bool IsValidScope { get; set; }
    public bool HasScope { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class AvailableScopesResponse
{
    public List<ScopeInfo> Scopes { get; set; } = new();
}

public class ScopeInfo
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

#endregion

using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Funtime.Identity.Api.Data;
using Funtime.Identity.Api.Models;
using Funtime.Identity.Api.Services;

namespace Funtime.Identity.Api.Controllers;

[ApiController]
[Route("auth/oauth")]
public class OAuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OAuthController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public OAuthController(
        ApplicationDbContext context,
        IJwtService jwtService,
        IConfiguration configuration,
        ILogger<OAuthController> logger,
        IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _jwtService = jwtService;
        _configuration = configuration;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Get available OAuth providers and their configuration status
    /// </summary>
    [HttpGet("providers")]
    public ActionResult<List<OAuthProviderInfo>> GetProviders()
    {
        var providers = new List<OAuthProviderInfo>
        {
            new() {
                Name = "google",
                DisplayName = "Google",
                IsConfigured = !string.IsNullOrEmpty(_configuration["OAuth:Google:ClientId"]),
                IsActive = _configuration.GetValue<bool>("OAuth:Google:Active")
            },
            new() {
                Name = "facebook",
                DisplayName = "Facebook",
                IsConfigured = !string.IsNullOrEmpty(_configuration["OAuth:Facebook:ClientId"]),
                IsActive = _configuration.GetValue<bool>("OAuth:Facebook:Active")
            },
            new() {
                Name = "apple",
                DisplayName = "Apple",
                IsConfigured = !string.IsNullOrEmpty(_configuration["OAuth:Apple:ClientId"]),
                IsActive = _configuration.GetValue<bool>("OAuth:Apple:Active")
            },
            new() {
                Name = "microsoft",
                DisplayName = "Microsoft",
                IsConfigured = !string.IsNullOrEmpty(_configuration["OAuth:Microsoft:ClientId"]),
                IsActive = _configuration.GetValue<bool>("OAuth:Microsoft:Active")
            }
        };

        // Only return providers that are both configured and active
        return Ok(providers.Where(p => p.IsConfigured && p.IsActive).ToList());
    }

    /// <summary>
    /// Start OAuth flow - redirects to provider's authorization page
    /// </summary>
    [HttpGet("{provider}/start")]
    public ActionResult StartOAuth(
        string provider,
        [FromQuery] string? returnUrl,
        [FromQuery] string? site)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
        var callbackUrl = $"{baseUrl}/auth/oauth/{provider}/callback";

        // Store state for CSRF protection and to pass returnUrl/site through OAuth flow
        var state = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(new OAuthState
        {
            ReturnUrl = returnUrl,
            SiteKey = site,
            Nonce = Guid.NewGuid().ToString()
        }));

        string authorizationUrl;

        switch (provider.ToLower())
        {
            case "google":
                var googleClientId = _configuration["OAuth:Google:ClientId"];
                if (string.IsNullOrEmpty(googleClientId))
                    return BadRequest(new { message = "Google OAuth is not configured" });

                authorizationUrl = "https://accounts.google.com/o/oauth2/v2/auth?" +
                    $"client_id={Uri.EscapeDataString(googleClientId)}&" +
                    $"redirect_uri={Uri.EscapeDataString(callbackUrl)}&" +
                    "response_type=code&" +
                    "scope=openid%20email%20profile&" +
                    $"state={Uri.EscapeDataString(state)}";
                break;

            case "facebook":
                var fbClientId = _configuration["OAuth:Facebook:ClientId"];
                if (string.IsNullOrEmpty(fbClientId))
                    return BadRequest(new { message = "Facebook OAuth is not configured" });

                authorizationUrl = "https://www.facebook.com/v18.0/dialog/oauth?" +
                    $"client_id={Uri.EscapeDataString(fbClientId)}&" +
                    $"redirect_uri={Uri.EscapeDataString(callbackUrl)}&" +
                    "scope=email,public_profile&" +
                    $"state={Uri.EscapeDataString(state)}";
                break;

            case "microsoft":
                var msClientId = _configuration["OAuth:Microsoft:ClientId"];
                if (string.IsNullOrEmpty(msClientId))
                    return BadRequest(new { message = "Microsoft OAuth is not configured" });

                authorizationUrl = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize?" +
                    $"client_id={Uri.EscapeDataString(msClientId)}&" +
                    $"redirect_uri={Uri.EscapeDataString(callbackUrl)}&" +
                    "response_type=code&" +
                    "scope=openid%20email%20profile&" +
                    $"state={Uri.EscapeDataString(state)}";
                break;

            case "apple":
                var appleClientId = _configuration["OAuth:Apple:ClientId"];
                if (string.IsNullOrEmpty(appleClientId))
                    return BadRequest(new { message = "Apple OAuth is not configured" });

                authorizationUrl = "https://appleid.apple.com/auth/authorize?" +
                    $"client_id={Uri.EscapeDataString(appleClientId)}&" +
                    $"redirect_uri={Uri.EscapeDataString(callbackUrl)}&" +
                    "response_type=code&" +
                    "scope=name%20email&" +
                    "response_mode=form_post&" +
                    $"state={Uri.EscapeDataString(state)}";
                break;

            default:
                return BadRequest(new { message = $"Unknown OAuth provider: {provider}" });
        }

        return Redirect(authorizationUrl);
    }

    /// <summary>
    /// OAuth callback - exchanges code for token and creates/logs in user
    /// </summary>
    [HttpGet("{provider}/callback")]
    [HttpPost("{provider}/callback")] // Apple uses POST
    public async Task<ActionResult> OAuthCallback(
        string provider,
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        [FromForm] string? codeForm, // Apple sends code in form
        [FromForm] string? stateForm) // Apple sends state in form
    {
        // Apple uses form POST
        code ??= codeForm;
        state ??= stateForm;

        if (!string.IsNullOrEmpty(error))
        {
            _logger.LogWarning("OAuth error from {Provider}: {Error}", provider, error);
            return RedirectToLoginWithError("OAuth authorization was denied");
        }

        if (string.IsNullOrEmpty(code))
        {
            return RedirectToLoginWithError("No authorization code received");
        }

        // Parse state
        OAuthState? oauthState = null;
        if (!string.IsNullOrEmpty(state))
        {
            try
            {
                oauthState = JsonSerializer.Deserialize<OAuthState>(
                    Convert.FromBase64String(state));
            }
            catch
            {
                _logger.LogWarning("Failed to parse OAuth state");
            }
        }

        try
        {
            var userInfo = await ExchangeCodeForUserInfo(provider, code);
            if (userInfo == null)
            {
                return RedirectToLoginWithError("Failed to get user information from provider");
            }

            // Find or create user
            var (user, isNewUser) = await FindOrCreateUser(provider, userInfo);

            // Generate JWT
            var token = _jwtService.GenerateToken(user);

            _logger.LogInformation(
                "{Action} user via {Provider}: {Email}",
                isNewUser ? "Created" : "Logged in",
                provider,
                userInfo.Email ?? userInfo.ProviderId);

            // Redirect back to frontend with token
            return RedirectWithToken(token, user, oauthState);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OAuth callback failed for {Provider}", provider);
            return RedirectToLoginWithError("Authentication failed. Please try again.");
        }
    }

    private async Task<OAuthUserInfo?> ExchangeCodeForUserInfo(string provider, string code)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
        var callbackUrl = $"{baseUrl}/auth/oauth/{provider}/callback";

        return provider.ToLower() switch
        {
            "google" => await GetGoogleUserInfo(code, callbackUrl),
            "facebook" => await GetFacebookUserInfo(code, callbackUrl),
            "microsoft" => await GetMicrosoftUserInfo(code, callbackUrl),
            "apple" => await GetAppleUserInfo(code, callbackUrl),
            _ => null
        };
    }

    private async Task<OAuthUserInfo?> GetGoogleUserInfo(string code, string redirectUri)
    {
        var clientId = _configuration["OAuth:Google:ClientId"];
        var clientSecret = _configuration["OAuth:Google:ClientSecret"];

        var httpClient = _httpClientFactory.CreateClient();

        // Exchange code for token
        var tokenResponse = await httpClient.PostAsync(
            "https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["code"] = code,
                ["client_id"] = clientId!,
                ["client_secret"] = clientSecret!,
                ["redirect_uri"] = redirectUri,
                ["grant_type"] = "authorization_code"
            }));

        if (!tokenResponse.IsSuccessStatusCode)
        {
            _logger.LogError("Google token exchange failed: {Status}", tokenResponse.StatusCode);
            return null;
        }

        var tokenJson = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = tokenJson.GetProperty("access_token").GetString();

        // Get user info
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        var userResponse = await httpClient.GetAsync(
            "https://www.googleapis.com/oauth2/v2/userinfo");

        if (!userResponse.IsSuccessStatusCode)
        {
            _logger.LogError("Google user info failed: {Status}", userResponse.StatusCode);
            return null;
        }

        var userJson = await userResponse.Content.ReadFromJsonAsync<JsonElement>();

        return new OAuthUserInfo
        {
            Provider = "google",
            ProviderId = userJson.GetProperty("id").GetString()!,
            Email = userJson.TryGetProperty("email", out var email) ? email.GetString() : null,
            Name = userJson.TryGetProperty("name", out var name) ? name.GetString() : null
        };
    }

    private async Task<OAuthUserInfo?> GetFacebookUserInfo(string code, string redirectUri)
    {
        var clientId = _configuration["OAuth:Facebook:ClientId"];
        var clientSecret = _configuration["OAuth:Facebook:ClientSecret"];

        var httpClient = _httpClientFactory.CreateClient();

        // Exchange code for token
        var tokenUrl = $"https://graph.facebook.com/v18.0/oauth/access_token?" +
            $"client_id={clientId}&" +
            $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
            $"client_secret={clientSecret}&" +
            $"code={code}";

        var tokenResponse = await httpClient.GetAsync(tokenUrl);
        if (!tokenResponse.IsSuccessStatusCode)
        {
            _logger.LogError("Facebook token exchange failed: {Status}", tokenResponse.StatusCode);
            return null;
        }

        var tokenJson = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = tokenJson.GetProperty("access_token").GetString();

        // Get user info
        var userResponse = await httpClient.GetAsync(
            $"https://graph.facebook.com/me?fields=id,name,email&access_token={accessToken}");

        if (!userResponse.IsSuccessStatusCode)
        {
            _logger.LogError("Facebook user info failed: {Status}", userResponse.StatusCode);
            return null;
        }

        var userJson = await userResponse.Content.ReadFromJsonAsync<JsonElement>();

        return new OAuthUserInfo
        {
            Provider = "facebook",
            ProviderId = userJson.GetProperty("id").GetString()!,
            Email = userJson.TryGetProperty("email", out var email) ? email.GetString() : null,
            Name = userJson.TryGetProperty("name", out var name) ? name.GetString() : null
        };
    }

    private async Task<OAuthUserInfo?> GetMicrosoftUserInfo(string code, string redirectUri)
    {
        var clientId = _configuration["OAuth:Microsoft:ClientId"];
        var clientSecret = _configuration["OAuth:Microsoft:ClientSecret"];

        var httpClient = _httpClientFactory.CreateClient();

        // Exchange code for token
        var tokenResponse = await httpClient.PostAsync(
            "https://login.microsoftonline.com/common/oauth2/v2.0/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["code"] = code,
                ["client_id"] = clientId!,
                ["client_secret"] = clientSecret!,
                ["redirect_uri"] = redirectUri,
                ["grant_type"] = "authorization_code"
            }));

        if (!tokenResponse.IsSuccessStatusCode)
        {
            _logger.LogError("Microsoft token exchange failed: {Status}", tokenResponse.StatusCode);
            return null;
        }

        var tokenJson = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = tokenJson.GetProperty("access_token").GetString();

        // Get user info
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        var userResponse = await httpClient.GetAsync("https://graph.microsoft.com/v1.0/me");

        if (!userResponse.IsSuccessStatusCode)
        {
            _logger.LogError("Microsoft user info failed: {Status}", userResponse.StatusCode);
            return null;
        }

        var userJson = await userResponse.Content.ReadFromJsonAsync<JsonElement>();

        return new OAuthUserInfo
        {
            Provider = "microsoft",
            ProviderId = userJson.GetProperty("id").GetString()!,
            Email = userJson.TryGetProperty("mail", out var mail) ? mail.GetString() :
                    userJson.TryGetProperty("userPrincipalName", out var upn) ? upn.GetString() : null,
            Name = userJson.TryGetProperty("displayName", out var name) ? name.GetString() : null
        };
    }

    private async Task<OAuthUserInfo?> GetAppleUserInfo(string code, string redirectUri)
    {
        // Apple Sign In requires generating a client secret JWT - simplified for now
        // Full implementation requires JWT generation with Apple's private key
        var clientId = _configuration["OAuth:Apple:ClientId"];
        var clientSecret = GenerateAppleClientSecret();

        if (string.IsNullOrEmpty(clientSecret))
        {
            _logger.LogError("Apple client secret generation failed");
            return null;
        }

        var httpClient = _httpClientFactory.CreateClient();

        // Exchange code for token
        var tokenResponse = await httpClient.PostAsync(
            "https://appleid.apple.com/auth/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["code"] = code,
                ["client_id"] = clientId!,
                ["client_secret"] = clientSecret,
                ["redirect_uri"] = redirectUri,
                ["grant_type"] = "authorization_code"
            }));

        if (!tokenResponse.IsSuccessStatusCode)
        {
            var error = await tokenResponse.Content.ReadAsStringAsync();
            _logger.LogError("Apple token exchange failed: {Status} - {Error}",
                tokenResponse.StatusCode, error);
            return null;
        }

        var tokenJson = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>();

        // Apple returns user info in the id_token (JWT)
        var idToken = tokenJson.GetProperty("id_token").GetString();
        var claims = DecodeJwtPayload(idToken!);

        return new OAuthUserInfo
        {
            Provider = "apple",
            ProviderId = claims.TryGetProperty("sub", out var sub) ? sub.GetString()! : "",
            Email = claims.TryGetProperty("email", out var email) ? email.GetString() : null,
            Name = null // Apple only provides name on first authorization
        };
    }

    private string? GenerateAppleClientSecret()
    {
        // Apple requires a JWT signed with your private key
        // This is a simplified placeholder - full implementation needed for production
        var teamId = _configuration["OAuth:Apple:TeamId"];
        var keyId = _configuration["OAuth:Apple:KeyId"];
        var privateKey = _configuration["OAuth:Apple:PrivateKey"];
        var clientId = _configuration["OAuth:Apple:ClientId"];

        if (string.IsNullOrEmpty(teamId) || string.IsNullOrEmpty(keyId) ||
            string.IsNullOrEmpty(privateKey) || string.IsNullOrEmpty(clientId))
        {
            return null;
        }

        // TODO: Implement proper Apple JWT client secret generation
        // This requires System.IdentityModel.Tokens.Jwt and ES256 signing
        _logger.LogWarning("Apple Sign In requires full client secret implementation");
        return null;
    }

    private static JsonElement DecodeJwtPayload(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length != 3)
            return new JsonElement();

        var payload = parts[1];
        // Handle base64url encoding
        payload = payload.Replace('-', '+').Replace('_', '/');
        switch (payload.Length % 4)
        {
            case 2: payload += "=="; break;
            case 3: payload += "="; break;
        }

        var bytes = Convert.FromBase64String(payload);
        return JsonSerializer.Deserialize<JsonElement>(bytes);
    }

    private async Task<(User user, bool isNewUser)> FindOrCreateUser(
        string provider, OAuthUserInfo userInfo)
    {
        // Check if this external login already exists
        var existingLogin = await _context.ExternalLogins
            .Include(e => e.User)
            .FirstOrDefaultAsync(e =>
                e.Provider == provider &&
                e.ProviderUserId == userInfo.ProviderId);

        if (existingLogin != null)
        {
            // Update last used and any changed info
            existingLogin.LastUsedAt = DateTime.UtcNow;
            existingLogin.ProviderEmail = userInfo.Email;
            existingLogin.ProviderDisplayName = userInfo.Name;
            existingLogin.User.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return (existingLogin.User, false);
        }

        // Check if a user with this email already exists
        User? user = null;
        if (!string.IsNullOrEmpty(userInfo.Email))
        {
            user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == userInfo.Email.ToLower());
        }

        bool isNewUser = user == null;

        if (user == null)
        {
            // Create new user
            user = new User
            {
                Email = userInfo.Email?.ToLower(),
                IsEmailVerified = !string.IsNullOrEmpty(userInfo.Email), // OAuth emails are verified
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }
        else
        {
            user.LastLoginAt = DateTime.UtcNow;
            if (!user.IsEmailVerified && !string.IsNullOrEmpty(userInfo.Email))
            {
                user.IsEmailVerified = true;
            }
        }

        // Create external login link
        var externalLogin = new ExternalLogin
        {
            UserId = user.Id,
            Provider = provider,
            ProviderUserId = userInfo.ProviderId,
            ProviderEmail = userInfo.Email,
            ProviderDisplayName = userInfo.Name,
            CreatedAt = DateTime.UtcNow,
            LastUsedAt = DateTime.UtcNow
        };
        _context.ExternalLogins.Add(externalLogin);
        await _context.SaveChangesAsync();

        return (user, isNewUser);
    }

    private RedirectResult RedirectWithToken(User user, string token, OAuthState? state)
    {
        // Determine where to redirect
        var frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:5173";

        // Build redirect URL
        var redirectUrl = state?.ReturnUrl;
        if (string.IsNullOrEmpty(redirectUrl))
        {
            // Default to frontend with token
            redirectUrl = $"{frontendUrl}/oauth-callback";
        }

        // Append token and site info
        var separator = redirectUrl.Contains('?') ? '&' : '?';
        var finalUrl = $"{redirectUrl}{separator}token={Uri.EscapeDataString(token)}";

        if (!string.IsNullOrEmpty(state?.SiteKey))
        {
            // Get user's role for the site
            var userSite = user.UserSites?
                .FirstOrDefault(us => us.SiteKey.Equals(state.SiteKey, StringComparison.OrdinalIgnoreCase) && us.IsActive);

            var siteRole = user.SystemRole == "SU" ? "admin" :
                          userSite?.Role ?? "member";
            var isSiteAdmin = user.SystemRole == "SU" || userSite?.Role == "admin";

            finalUrl += $"&siteRole={siteRole}&isSiteAdmin={isSiteAdmin.ToString().ToLower()}";
        }

        return Redirect(finalUrl);
    }

    private ActionResult RedirectWithToken(string token, User user, OAuthState? state)
    {
        // Determine where to redirect
        var frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:5173";

        // Build redirect URL
        string redirectUrl;
        if (!string.IsNullOrEmpty(state?.ReturnUrl))
        {
            redirectUrl = state.ReturnUrl;
        }
        else
        {
            redirectUrl = $"{frontendUrl}/oauth-callback";
        }

        // Append token
        var separator = redirectUrl.Contains('?') ? '&' : '?';
        var finalUrl = $"{redirectUrl}{separator}token={Uri.EscapeDataString(token)}";

        if (!string.IsNullOrEmpty(state?.SiteKey))
        {
            // Get user's role for the site
            var siteRole = user.SystemRole == "SU" ? "admin" : "member";
            var isSiteAdmin = user.SystemRole == "SU";

            // Load user sites if needed
            if (user.SystemRole != "SU")
            {
                var userSite = _context.UserSites
                    .FirstOrDefault(us => us.UserId == user.Id &&
                        us.SiteKey.ToLower() == state.SiteKey.ToLower() && us.IsActive);
                if (userSite != null)
                {
                    siteRole = userSite.Role;
                    isSiteAdmin = userSite.Role == "admin";
                }
            }

            finalUrl += $"&siteRole={siteRole}&isSiteAdmin={isSiteAdmin.ToString().ToLower()}";
        }

        return Redirect(finalUrl);
    }

    private RedirectResult RedirectToLoginWithError(string error)
    {
        var frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:5173";
        return Redirect($"{frontendUrl}/login?error={Uri.EscapeDataString(error)}");
    }
}

public class OAuthProviderInfo
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsConfigured { get; set; }
    public bool IsActive { get; set; }
}

public class OAuthState
{
    public string? ReturnUrl { get; set; }
    public string? SiteKey { get; set; }
    public string? Nonce { get; set; }
}

public class OAuthUserInfo
{
    public string Provider { get; set; } = string.Empty;
    public string ProviderId { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Name { get; set; }
}

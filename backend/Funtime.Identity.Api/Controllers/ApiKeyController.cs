using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Funtime.Identity.Api.DTOs;
using Funtime.Identity.Api.Models;
using Funtime.Identity.Api.Services;

namespace Funtime.Identity.Api.Controllers;

[ApiController]
[Route("admin/api-keys")]
[Authorize(Roles = "SU")]
public class ApiKeyController : ControllerBase
{
    private readonly IApiKeyService _apiKeyService;
    private readonly ILogger<ApiKeyController> _logger;

    public ApiKeyController(
        IApiKeyService apiKeyService,
        ILogger<ApiKeyController> logger)
    {
        _apiKeyService = apiKeyService;
        _logger = logger;
    }

    /// <summary>
    /// Get all API keys (masked)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ApiKeyResponse>>> GetAll()
    {
        var keys = await _apiKeyService.GetAllAsync();
        return Ok(keys);
    }

    /// <summary>
    /// Get available scopes
    /// </summary>
    [HttpGet("scopes")]
    public ActionResult<ApiScopesResponse> GetScopes()
    {
        var scopes = new ApiScopesResponse
        {
            Scopes = new List<ApiScopeInfo>
            {
                new() { Name = ApiScopes.AuthValidate, Description = "Validate JWT tokens", Category = "Auth" },
                new() { Name = ApiScopes.AuthSync, Description = "Sync user authentication", Category = "Auth" },
                new() { Name = ApiScopes.UsersRead, Description = "Read user information", Category = "Users" },
                new() { Name = ApiScopes.UsersWrite, Description = "Update user information", Category = "Users" },
                new() { Name = ApiScopes.AssetsRead, Description = "Read/download assets", Category = "Assets" },
                new() { Name = ApiScopes.AssetsWrite, Description = "Upload/delete assets", Category = "Assets" },
                new() { Name = ApiScopes.SitesRead, Description = "Read site information", Category = "Sites" },
                new() { Name = ApiScopes.PushSend, Description = "Send push notifications", Category = "Notifications" },
                new() { Name = ApiScopes.Admin, Description = "Full administrative access", Category = "Admin" }
            }
        };
        return Ok(scopes);
    }

    /// <summary>
    /// Create a new API key
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiKeyCreatedResponse>> Create([FromBody] CreateApiKeyRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Validate scopes
        if (request.Scopes.Count > 0)
        {
            var invalidScopes = request.Scopes.Where(s => !ApiScopes.AllScopes.Contains(s)).ToList();
            if (invalidScopes.Count > 0)
            {
                return BadRequest(new { message = $"Invalid scopes: {string.Join(", ", invalidScopes)}" });
            }
        }

        try
        {
            var createdBy = User.Identity?.Name ?? "admin";
            var result = await _apiKeyService.CreateAsync(request, createdBy);

            _logger.LogInformation("API key created for partner {PartnerKey} by {User}",
                request.PartnerKey, createdBy);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create API key for partner {PartnerKey}", request.PartnerKey);

            if (ex.Message.Contains("already exists"))
            {
                return BadRequest(new { message = "A partner with this key already exists." });
            }

            return BadRequest(new { message = "Failed to create API key." });
        }
    }

    /// <summary>
    /// Update an API key
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiKeyResponse>> Update(int id, [FromBody] UpdateApiKeyRequest request)
    {
        // Validate scopes if provided
        if (request.Scopes != null && request.Scopes.Count > 0)
        {
            var invalidScopes = request.Scopes.Where(s => !ApiScopes.AllScopes.Contains(s)).ToList();
            if (invalidScopes.Count > 0)
            {
                return BadRequest(new { message = $"Invalid scopes: {string.Join(", ", invalidScopes)}" });
            }
        }

        var result = await _apiKeyService.UpdateAsync(id, request);

        if (result == null)
        {
            return NotFound(new { message = "API key not found." });
        }

        _logger.LogInformation("API key {Id} updated by {User}", id, User.Identity?.Name);

        return Ok(result);
    }

    /// <summary>
    /// Delete an API key
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var success = await _apiKeyService.DeleteAsync(id);

        if (!success)
        {
            return NotFound(new { message = "API key not found." });
        }

        _logger.LogInformation("API key {Id} deleted by {User}", id, User.Identity?.Name);

        return Ok(new { success = true, message = "API key deleted." });
    }

    /// <summary>
    /// Regenerate an API key (creates new key, invalidates old one)
    /// </summary>
    [HttpPost("{id}/regenerate")]
    public async Task<ActionResult<ApiKeyCreatedResponse>> Regenerate(int id)
    {
        var result = await _apiKeyService.RegenerateAsync(id);

        if (result == null)
        {
            return NotFound(new { message = "API key not found." });
        }

        _logger.LogInformation("API key {Id} regenerated by {User}", id, User.Identity?.Name);

        return Ok(result);
    }

    /// <summary>
    /// Toggle API key active status
    /// </summary>
    [HttpPost("{id}/toggle")]
    public async Task<ActionResult<ApiKeyResponse>> Toggle(int id)
    {
        // Get current state
        var keys = await _apiKeyService.GetAllAsync();
        var key = keys.FirstOrDefault(k => k.Id == id);

        if (key == null)
        {
            return NotFound(new { message = "API key not found." });
        }

        var result = await _apiKeyService.UpdateAsync(id, new UpdateApiKeyRequest
        {
            IsActive = !key.IsActive
        });

        _logger.LogInformation("API key {Id} toggled to {Status} by {User}",
            id, result?.IsActive == true ? "active" : "inactive", User.Identity?.Name);

        return Ok(result);
    }
}

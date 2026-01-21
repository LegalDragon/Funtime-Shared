using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Dapper;
using Funtime.Identity.Api.Models;
using Funtime.Identity.Api.DTOs;

namespace Funtime.Identity.Api.Services;

public interface IApiKeyService
{
    /// <summary>
    /// Validate an API key and return its details if valid
    /// </summary>
    Task<ApiKey?> ValidateKeyAsync(string apiKey);

    /// <summary>
    /// Check if an API key has a specific scope
    /// </summary>
    Task<bool> HasScopeAsync(string apiKey, string scope);

    /// <summary>
    /// Record usage of an API key
    /// </summary>
    Task RecordUsageAsync(string apiKey);

    /// <summary>
    /// Get all API keys (for admin)
    /// </summary>
    Task<List<ApiKeyResponse>> GetAllAsync();

    /// <summary>
    /// Create a new API key
    /// </summary>
    Task<ApiKeyCreatedResponse> CreateAsync(CreateApiKeyRequest request, string? createdBy);

    /// <summary>
    /// Update an API key
    /// </summary>
    Task<ApiKeyResponse?> UpdateAsync(int id, UpdateApiKeyRequest request);

    /// <summary>
    /// Delete an API key
    /// </summary>
    Task<bool> DeleteAsync(int id);

    /// <summary>
    /// Regenerate an API key (creates new key for existing partner)
    /// </summary>
    Task<ApiKeyCreatedResponse?> RegenerateAsync(int id);

    /// <summary>
    /// Invalidate cached API key
    /// </summary>
    void InvalidateCache(string apiKey);

    /// <summary>
    /// Clear all cached API keys
    /// </summary>
    void ClearCache();
}

public class ApiKeyService : IApiKeyService
{
    private readonly string _connectionString;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ApiKeyService> _logger;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private const string CacheKeyPrefix = "apikey_";

    public ApiKeyService(
        IConfiguration configuration,
        IMemoryCache cache,
        ILogger<ApiKeyService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection not configured");
        _cache = cache;
        _logger = logger;
    }

    public async Task<ApiKey?> ValidateKeyAsync(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey)) return null;

        var cacheKey = CacheKeyPrefix + apiKey;

        // Try cache first
        if (_cache.TryGetValue(cacheKey, out ApiKey? cachedKey))
        {
            return cachedKey?.IsValid() == true ? cachedKey : null;
        }

        // Query database
        using var conn = new SqlConnection(_connectionString);
        var key = await conn.QuerySingleOrDefaultAsync<ApiKeyDbRow>(
            "csp_ApiKey_GetByKey",
            new { ApiKey = apiKey },
            commandType: System.Data.CommandType.StoredProcedure);

        if (key == null)
        {
            // Cache negative result for a short time to prevent hammering
            _cache.Set(cacheKey, (ApiKey?)null, TimeSpan.FromSeconds(30));
            return null;
        }

        var apiKeyModel = MapToModel(key);

        // Cache valid keys
        _cache.Set(cacheKey, apiKeyModel, CacheDuration);

        return apiKeyModel.IsValid() ? apiKeyModel : null;
    }

    public async Task<bool> HasScopeAsync(string apiKey, string scope)
    {
        var key = await ValidateKeyAsync(apiKey);
        return key?.HasScope(scope) ?? false;
    }

    public async Task RecordUsageAsync(string apiKey)
    {
        try
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.ExecuteAsync(
                "csp_ApiKey_RecordUsage",
                new { ApiKey = apiKey },
                commandType: System.Data.CommandType.StoredProcedure);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record API key usage");
        }
    }

    public async Task<List<ApiKeyResponse>> GetAllAsync()
    {
        using var conn = new SqlConnection(_connectionString);
        var rows = await conn.QueryAsync<ApiKeyListDbRow>(
            "csp_ApiKey_GetAll",
            commandType: System.Data.CommandType.StoredProcedure);

        return rows.Select(MapToResponse).ToList();
    }

    public async Task<ApiKeyCreatedResponse> CreateAsync(CreateApiKeyRequest request, string? createdBy)
    {
        var (apiKey, keyPrefix) = GenerateApiKey(request.PartnerKey);

        using var conn = new SqlConnection(_connectionString);
        var row = await conn.QuerySingleAsync<ApiKeyDbRow>(
            "csp_ApiKey_Create",
            new
            {
                request.PartnerKey,
                request.PartnerName,
                ApiKey = apiKey,
                KeyPrefix = keyPrefix,
                Scopes = request.Scopes?.Count > 0 ? JsonSerializer.Serialize(request.Scopes) : null,
                AllowedIPs = request.AllowedIPs?.Count > 0 ? JsonSerializer.Serialize(request.AllowedIPs) : null,
                AllowedOrigins = request.AllowedOrigins?.Count > 0 ? JsonSerializer.Serialize(request.AllowedOrigins) : null,
                request.RateLimitPerMinute,
                request.ExpiresAt,
                request.Description,
                CreatedBy = createdBy
            },
            commandType: System.Data.CommandType.StoredProcedure);

        _logger.LogInformation("API key created for partner {PartnerKey} by {CreatedBy}", request.PartnerKey, createdBy);

        return MapToCreatedResponse(row, apiKey);
    }

    public async Task<ApiKeyResponse?> UpdateAsync(int id, UpdateApiKeyRequest request)
    {
        using var conn = new SqlConnection(_connectionString);
        var row = await conn.QuerySingleOrDefaultAsync<ApiKeyListDbRow>(
            "csp_ApiKey_Update",
            new
            {
                Id = id,
                request.PartnerName,
                Scopes = request.Scopes != null ? JsonSerializer.Serialize(request.Scopes) : null,
                AllowedIPs = request.AllowedIPs != null ? JsonSerializer.Serialize(request.AllowedIPs) : null,
                AllowedOrigins = request.AllowedOrigins != null ? JsonSerializer.Serialize(request.AllowedOrigins) : null,
                request.RateLimitPerMinute,
                request.IsActive,
                request.ExpiresAt,
                request.Description
            },
            commandType: System.Data.CommandType.StoredProcedure);

        if (row != null)
        {
            // Clear cache for this key
            ClearCache();
            _logger.LogInformation("API key {Id} updated", id);
        }

        return row != null ? MapToResponse(row) : null;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var conn = new SqlConnection(_connectionString);
        var result = await conn.QuerySingleAsync<int>(
            "csp_ApiKey_Delete",
            new { Id = id },
            commandType: System.Data.CommandType.StoredProcedure);

        if (result > 0)
        {
            ClearCache();
            _logger.LogInformation("API key {Id} deleted", id);
        }

        return result > 0;
    }

    public async Task<ApiKeyCreatedResponse?> RegenerateAsync(int id)
    {
        // First get the existing key to get partner key
        using var conn = new SqlConnection(_connectionString);
        var existing = await conn.QuerySingleOrDefaultAsync<ApiKeyListDbRow>(
            "SELECT PartnerKey FROM ApiKeys WHERE Id = @Id",
            new { Id = id });

        if (existing == null) return null;

        var (newApiKey, keyPrefix) = GenerateApiKey(existing.PartnerKey);

        var row = await conn.QuerySingleAsync<ApiKeyDbRow>(
            "csp_ApiKey_Regenerate",
            new
            {
                Id = id,
                NewApiKey = newApiKey,
                NewKeyPrefix = keyPrefix
            },
            commandType: System.Data.CommandType.StoredProcedure);

        ClearCache();
        _logger.LogInformation("API key {Id} regenerated for partner {PartnerKey}", id, existing.PartnerKey);

        return MapToCreatedResponse(row, newApiKey);
    }

    public void InvalidateCache(string apiKey)
    {
        _cache.Remove(CacheKeyPrefix + apiKey);
    }

    public void ClearCache()
    {
        // MemoryCache doesn't have a clear all method, so we rely on expiration
        // For immediate clearing, we'd need to track keys or use a different cache
        _logger.LogDebug("API key cache clear requested");
    }

    /// <summary>
    /// Generate a new API key with prefix
    /// </summary>
    private static (string apiKey, string keyPrefix) GenerateApiKey(string partnerKey)
    {
        // Create a prefix like "pk_comm_" from partner key
        var shortPartner = partnerKey.Length > 4 ? partnerKey[..4] : partnerKey;
        var keyPrefix = $"pk_{shortPartner}_";

        // Generate random part (32 bytes = 64 hex chars, but we'll use base64url for shorter keys)
        var randomBytes = RandomNumberGenerator.GetBytes(32);
        var randomPart = Convert.ToBase64String(randomBytes)
            .Replace("+", "")
            .Replace("/", "")
            .Replace("=", "")
            [..32]; // Take first 32 chars

        var apiKey = keyPrefix + randomPart;
        return (apiKey, keyPrefix);
    }

    #region Mapping Helpers

    private static ApiKey MapToModel(ApiKeyDbRow row) => new()
    {
        Id = row.Id,
        PartnerKey = row.PartnerKey,
        PartnerName = row.PartnerName,
        Key = row.ApiKey,
        KeyPrefix = row.KeyPrefix,
        Scopes = row.Scopes,
        AllowedIPs = row.AllowedIPs,
        AllowedOrigins = row.AllowedOrigins,
        RateLimitPerMinute = row.RateLimitPerMinute,
        IsActive = row.IsActive,
        ExpiresAt = row.ExpiresAt,
        CreatedAt = row.CreatedAt,
        UpdatedAt = row.UpdatedAt,
        LastUsedAt = row.LastUsedAt,
        UsageCount = row.UsageCount,
        Description = row.Description,
        CreatedBy = row.CreatedBy
    };

    private static ApiKeyResponse MapToResponse(ApiKeyListDbRow row) => new()
    {
        Id = row.Id,
        PartnerKey = row.PartnerKey,
        PartnerName = row.PartnerName,
        KeyMasked = row.ApiKeyMasked ?? (row.KeyPrefix + "..."),
        KeyPrefix = row.KeyPrefix,
        Scopes = ParseJsonArray(row.Scopes),
        AllowedIPs = ParseJsonArrayNullable(row.AllowedIPs),
        AllowedOrigins = ParseJsonArrayNullable(row.AllowedOrigins),
        RateLimitPerMinute = row.RateLimitPerMinute,
        IsActive = row.IsActive,
        ExpiresAt = row.ExpiresAt,
        CreatedAt = row.CreatedAt,
        UpdatedAt = row.UpdatedAt,
        LastUsedAt = row.LastUsedAt,
        UsageCount = row.UsageCount,
        Description = row.Description,
        CreatedBy = row.CreatedBy
    };

    private static ApiKeyCreatedResponse MapToCreatedResponse(ApiKeyDbRow row, string fullApiKey) => new()
    {
        Id = row.Id,
        PartnerKey = row.PartnerKey,
        PartnerName = row.PartnerName,
        ApiKey = fullApiKey,
        KeyMasked = row.KeyPrefix + "...",
        KeyPrefix = row.KeyPrefix,
        Scopes = ParseJsonArray(row.Scopes),
        AllowedIPs = ParseJsonArrayNullable(row.AllowedIPs),
        AllowedOrigins = ParseJsonArrayNullable(row.AllowedOrigins),
        RateLimitPerMinute = row.RateLimitPerMinute,
        IsActive = row.IsActive,
        ExpiresAt = row.ExpiresAt,
        CreatedAt = row.CreatedAt,
        UpdatedAt = row.UpdatedAt,
        LastUsedAt = row.LastUsedAt,
        UsageCount = row.UsageCount,
        Description = row.Description,
        CreatedBy = row.CreatedBy
    };

    private static List<string> ParseJsonArray(string? json)
    {
        if (string.IsNullOrEmpty(json)) return new List<string>();
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private static List<string>? ParseJsonArrayNullable(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json);
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region Database Row Classes

    private class ApiKeyDbRow
    {
        public int Id { get; set; }
        public string PartnerKey { get; set; } = string.Empty;
        public string PartnerName { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string KeyPrefix { get; set; } = string.Empty;
        public string? Scopes { get; set; }
        public string? AllowedIPs { get; set; }
        public string? AllowedOrigins { get; set; }
        public int RateLimitPerMinute { get; set; }
        public bool IsActive { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastUsedAt { get; set; }
        public long UsageCount { get; set; }
        public string? Description { get; set; }
        public string? CreatedBy { get; set; }
    }

    private class ApiKeyListDbRow : ApiKeyDbRow
    {
        public string? ApiKeyMasked { get; set; }
    }

    #endregion
}

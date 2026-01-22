using System.ComponentModel.DataAnnotations;

namespace Funtime.Identity.Api.DTOs;

#region API Key DTOs

/// <summary>
/// Response for API key list (masked key)
/// </summary>
public class ApiKeyResponse
{
    public int Id { get; set; }
    public string PartnerKey { get; set; } = string.Empty;
    public string PartnerName { get; set; } = string.Empty;
    public string KeyMasked { get; set; } = string.Empty;  // e.g., "pk_comm_..."
    public string KeyPrefix { get; set; } = string.Empty;
    public List<string> Scopes { get; set; } = new();
    public List<string>? AllowedIPs { get; set; }
    public List<string>? AllowedOrigins { get; set; }
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

/// <summary>
/// Response for newly created API key (includes full key - only shown once)
/// </summary>
public class ApiKeyCreatedResponse : ApiKeyResponse
{
    public string ApiKey { get; set; } = string.Empty;  // Full key, only returned on create/regenerate
}

/// <summary>
/// Request to create a new API key
/// </summary>
public class CreateApiKeyRequest
{
    [Required]
    [MaxLength(50)]
    [RegularExpression(@"^[a-z0-9\-]+$", ErrorMessage = "Partner key must be lowercase alphanumeric with hyphens only")]
    public string PartnerKey { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string PartnerName { get; set; } = string.Empty;

    /// <summary>
    /// List of scopes to grant (e.g., ["auth:validate", "assets:read"])
    /// </summary>
    public List<string> Scopes { get; set; } = new();

    /// <summary>
    /// Optional list of allowed IP addresses or CIDR ranges
    /// </summary>
    public List<string>? AllowedIPs { get; set; }

    /// <summary>
    /// Optional list of allowed origins for CORS
    /// </summary>
    public List<string>? AllowedOrigins { get; set; }

    public int RateLimitPerMinute { get; set; } = 60;

    /// <summary>
    /// Optional expiration date
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }
}

/// <summary>
/// Request to update an API key
/// </summary>
public class UpdateApiKeyRequest
{
    [MaxLength(100)]
    public string? PartnerName { get; set; }

    public List<string>? Scopes { get; set; }

    public List<string>? AllowedIPs { get; set; }

    public List<string>? AllowedOrigins { get; set; }

    public int? RateLimitPerMinute { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? ExpiresAt { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }
}

/// <summary>
/// Response for available scopes
/// </summary>
public class ApiScopesResponse
{
    public List<ApiScopeInfo> Scopes { get; set; } = new();
}

public class ApiScopeInfo
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

#endregion

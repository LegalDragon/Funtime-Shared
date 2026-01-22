using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Funtime.Identity.Api.Models;

/// <summary>
/// Represents an API key for partner authentication
/// </summary>
[Table("ApiKeys")]
public class ApiKey
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string PartnerKey { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string PartnerName { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string Key { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string KeyPrefix { get; set; } = string.Empty;

    /// <summary>
    /// JSON array of allowed scopes (e.g., ["auth:validate", "users:read"])
    /// </summary>
    public string? Scopes { get; set; }

    /// <summary>
    /// JSON array of allowed IP addresses or CIDR ranges
    /// </summary>
    public string? AllowedIPs { get; set; }

    /// <summary>
    /// JSON array of allowed origins for CORS
    /// </summary>
    public string? AllowedOrigins { get; set; }

    public int RateLimitPerMinute { get; set; } = 60;

    public bool IsActive { get; set; } = true;

    public DateTime? ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? LastUsedAt { get; set; }

    public long UsageCount { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    // Helper methods for JSON fields
    [NotMapped]
    public List<string> ScopesList
    {
        get
        {
            if (string.IsNullOrEmpty(Scopes)) return new List<string>();
            try
            {
                return JsonSerializer.Deserialize<List<string>>(Scopes) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }
    }

    [NotMapped]
    public List<string> AllowedIPsList
    {
        get
        {
            if (string.IsNullOrEmpty(AllowedIPs)) return new List<string>();
            try
            {
                return JsonSerializer.Deserialize<List<string>>(AllowedIPs) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }
    }

    [NotMapped]
    public List<string> AllowedOriginsList
    {
        get
        {
            if (string.IsNullOrEmpty(AllowedOrigins)) return new List<string>();
            try
            {
                return JsonSerializer.Deserialize<List<string>>(AllowedOrigins) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }
    }

    /// <summary>
    /// Check if this API key has a specific scope
    /// </summary>
    public bool HasScope(string scope)
    {
        var scopes = ScopesList;
        // "admin" scope grants all permissions
        if (scopes.Contains("admin")) return true;
        return scopes.Contains(scope);
    }

    /// <summary>
    /// Check if this API key is valid (active, not expired)
    /// </summary>
    public bool IsValid()
    {
        if (!IsActive) return false;
        if (ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow) return false;
        return true;
    }
}

/// <summary>
/// Available API scopes
/// </summary>
public static class ApiScopes
{
    // Auth scopes
    public const string AuthValidate = "auth:validate";
    public const string AuthSync = "auth:sync";

    // User scopes
    public const string UsersRead = "users:read";
    public const string UsersWrite = "users:write";

    // Asset scopes
    public const string AssetsRead = "assets:read";
    public const string AssetsWrite = "assets:write";

    // Site scopes
    public const string SitesRead = "sites:read";

    // Push notification scopes
    public const string PushSend = "push:send";

    // Admin scope (full access)
    public const string Admin = "admin";

    /// <summary>
    /// All available scopes
    /// </summary>
    public static readonly string[] AllScopes = new[]
    {
        AuthValidate, AuthSync,
        UsersRead, UsersWrite,
        AssetsRead, AssetsWrite,
        SitesRead,
        PushSend,
        Admin
    };
}

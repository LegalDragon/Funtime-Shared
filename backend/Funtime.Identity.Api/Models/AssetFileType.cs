using System.ComponentModel.DataAnnotations;

namespace Funtime.Identity.Api.Models;

/// <summary>
/// Represents a configurable file type for asset uploads
/// </summary>
public class AssetFileType
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// MIME type (e.g., "image/jpeg", "video/mp4")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string MimeType { get; set; } = string.Empty;

    /// <summary>
    /// Comma-separated file extensions (e.g., ".jpg,.jpeg")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Extensions { get; set; } = string.Empty;

    /// <summary>
    /// Category: image, video, audio, document
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Maximum file size in megabytes
    /// </summary>
    public int MaxSizeMB { get; set; } = 10;

    /// <summary>
    /// Whether this file type is currently enabled for uploads
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Human-readable display name
    /// </summary>
    [MaxLength(50)]
    public string? DisplayName { get; set; }

    /// <summary>
    /// When the file type was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the file type was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// DTO for creating a new file type
/// </summary>
public class CreateAssetFileTypeRequest
{
    [Required]
    [MaxLength(100)]
    public string MimeType { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Extensions { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Category { get; set; } = string.Empty;

    public int MaxSizeMB { get; set; } = 10;

    public bool IsEnabled { get; set; } = true;

    [MaxLength(50)]
    public string? DisplayName { get; set; }
}

/// <summary>
/// DTO for updating a file type
/// </summary>
public class UpdateAssetFileTypeRequest
{
    [Required]
    [MaxLength(100)]
    public string MimeType { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Extensions { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Category { get; set; } = string.Empty;

    public int MaxSizeMB { get; set; } = 10;

    public bool IsEnabled { get; set; } = true;

    [MaxLength(50)]
    public string? DisplayName { get; set; }
}

/// <summary>
/// Response containing grouped file types for upload modal
/// </summary>
public class AssetFileTypesResponse
{
    public List<AssetFileType> FileTypes { get; set; } = new();

    /// <summary>
    /// Comma-separated accept string for file input (e.g., "image/*,video/*,.pdf,.doc")
    /// </summary>
    public string AcceptString { get; set; } = string.Empty;

    /// <summary>
    /// File types grouped by category
    /// </summary>
    public Dictionary<string, List<AssetFileType>> ByCategory { get; set; } = new();
}

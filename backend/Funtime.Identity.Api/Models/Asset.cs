using System.ComponentModel.DataAnnotations;

namespace Funtime.Identity.Api.Models;

/// <summary>
/// Asset types for categorizing content
/// </summary>
public static class AssetTypes
{
    public const string Image = "image";
    public const string Video = "video";
    public const string Document = "document";
    public const string Audio = "audio";
    public const string Link = "link";
}

/// <summary>
/// Represents an uploaded asset (image, document, etc.) or external link
/// </summary>
public class Asset
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Asset type: image, video, document, audio, link
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string AssetType { get; set; } = AssetTypes.Image;

    /// <summary>
    /// Original filename (for uploads) or title (for links)
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// MIME type (e.g., image/png, application/pdf, video/mp4)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes (0 for external links)
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Storage location URL (S3 URL or local path) - for uploaded files
    /// </summary>
    [MaxLength(1000)]
    public string StorageUrl { get; set; } = string.Empty;

    /// <summary>
    /// External URL (YouTube, Vimeo, etc.) - for linked assets
    /// </summary>
    [MaxLength(2000)]
    public string? ExternalUrl { get; set; }

    /// <summary>
    /// Thumbnail URL for videos or external content
    /// </summary>
    [MaxLength(1000)]
    public string? ThumbnailUrl { get; set; }

    /// <summary>
    /// Storage type: "local", "s3", or "external"
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string StorageType { get; set; } = "local";

    /// <summary>
    /// Category/folder for organizing assets (e.g., "logos", "avatars", "documents")
    /// </summary>
    [MaxLength(50)]
    public string? Category { get; set; }

    /// <summary>
    /// Site key for organizing assets by site (e.g., "pickleball-community")
    /// </summary>
    [MaxLength(50)]
    public string? SiteKey { get; set; }

    /// <summary>
    /// User who uploaded the asset (nullable for system uploads)
    /// </summary>
    public int? UploadedBy { get; set; }

    /// <summary>
    /// When the asset was uploaded
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the asset is publicly accessible without authentication
    /// </summary>
    public bool IsPublic { get; set; } = true;
}

using System.ComponentModel.DataAnnotations;

namespace Funtime.Identity.Api.Models;

/// <summary>
/// Represents an uploaded asset (image, document, etc.)
/// </summary>
public class Asset
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Original filename
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// MIME type (e.g., image/png, application/pdf)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Storage location URL (S3 URL or local path)
    /// </summary>
    [Required]
    [MaxLength(1000)]
    public string StorageUrl { get; set; } = string.Empty;

    /// <summary>
    /// Storage type: "local" or "s3"
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

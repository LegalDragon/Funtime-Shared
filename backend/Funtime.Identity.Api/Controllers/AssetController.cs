using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Dapper;
using System.Security.Claims;
using Funtime.Identity.Api.Auth;
using Funtime.Identity.Api.Data;
using Funtime.Identity.Api.Models;
using Funtime.Identity.Api.Services;

namespace Funtime.Identity.Api.Controllers;

[ApiController]
[Route("asset")]
public class AssetController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _storageService;
    private readonly ILogger<AssetController> _logger;
    private readonly string _connectionString;

    // Cache for file types to avoid DB hits on every upload
    private static List<AssetFileType>? _cachedFileTypes;
    private static DateTime _cacheExpiry = DateTime.MinValue;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public AssetController(
        ApplicationDbContext context,
        IFileStorageService storageService,
        ILogger<AssetController> logger,
        IConfiguration configuration)
    {
        _context = context;
        _storageService = storageService;
        _logger = logger;
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection not configured");
    }

    private SqlConnection CreateConnection() => new SqlConnection(_connectionString);

    /// <summary>
    /// Get enabled file types from database with caching
    /// </summary>
    private async Task<List<AssetFileType>> GetEnabledFileTypesAsync()
    {
        if (_cachedFileTypes != null && DateTime.UtcNow < _cacheExpiry)
        {
            return _cachedFileTypes;
        }

        try
        {
            using var conn = CreateConnection();
            var fileTypes = (await conn.QueryAsync<AssetFileType>("exec dbo.csp_AssetFileTypes_GetEnabled")).ToList();
            _cachedFileTypes = fileTypes;
            _cacheExpiry = DateTime.UtcNow.Add(CacheDuration);
            return fileTypes;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load file types from DB, using fallback");
            // Return fallback types if DB fails
            return GetFallbackFileTypes();
        }
    }

    /// <summary>
    /// Fallback file types if database is unavailable
    /// </summary>
    private static List<AssetFileType> GetFallbackFileTypes()
    {
        return new List<AssetFileType>
        {
            // Images
            new() { MimeType = "image/jpeg", Extensions = ".jpg,.jpeg", Category = "image", MaxSizeMB = 10 },
            new() { MimeType = "image/png", Extensions = ".png", Category = "image", MaxSizeMB = 10 },
            new() { MimeType = "image/gif", Extensions = ".gif", Category = "image", MaxSizeMB = 10 },
            new() { MimeType = "image/webp", Extensions = ".webp", Category = "image", MaxSizeMB = 10 },
            new() { MimeType = "image/svg+xml", Extensions = ".svg", Category = "image", MaxSizeMB = 10 },
            // Videos
            new() { MimeType = "video/mp4", Extensions = ".mp4", Category = "video", MaxSizeMB = 100 },
            new() { MimeType = "video/webm", Extensions = ".webm", Category = "video", MaxSizeMB = 100 },
            new() { MimeType = "video/quicktime", Extensions = ".mov", Category = "video", MaxSizeMB = 100 },
            // Audio
            new() { MimeType = "audio/mpeg", Extensions = ".mp3", Category = "audio", MaxSizeMB = 10 },
            new() { MimeType = "audio/wav", Extensions = ".wav", Category = "audio", MaxSizeMB = 10 },
            // Documents
            new() { MimeType = "application/pdf", Extensions = ".pdf", Category = "document", MaxSizeMB = 10 },
            new() { MimeType = "text/markdown", Extensions = ".md", Category = "document", MaxSizeMB = 10 },
            new() { MimeType = "text/html", Extensions = ".html,.htm", Category = "document", MaxSizeMB = 10 }
        };
    }

    /// <summary>
    /// Upload an asset and get back the asset ID (supports API key with assets:write scope)
    /// </summary>
    [HttpPost("upload")]
    [ApiKeyAuthorize(ApiScopes.AssetsWrite, AllowJwt = true)]
    [RequestSizeLimit(150 * 1024 * 1024)] // 150MB limit for video uploads
    public async Task<ActionResult<AssetUploadResponse>> Upload(
        IFormFile file,
        [FromQuery] string? assetType = null,
        [FromQuery] string? category = null,
        [FromQuery] string? siteKey = null,
        [FromQuery] bool isPublic = true)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file uploaded." });
        }

        // Get allowed file types from database
        var fileTypes = await GetEnabledFileTypesAsync();
        var contentType = file.ContentType.ToLower();
        var fileExtension = Path.GetExtension(file.FileName)?.ToLower() ?? "";

        // Find matching file type by MIME type first
        var matchingFileType = fileTypes.FirstOrDefault(ft => ft.MimeType.ToLower() == contentType);

        // If no MIME type match, try matching by file extension (browsers often send wrong MIME types)
        if (matchingFileType == null && !string.IsNullOrEmpty(fileExtension))
        {
            matchingFileType = fileTypes.FirstOrDefault(ft =>
                ft.Extensions.ToLower().Split(',').Select(e => e.Trim()).Contains(fileExtension));
        }

        if (matchingFileType == null)
        {
            // Build friendly error message with allowed types grouped by category
            var allowedByCategory = fileTypes
                .GroupBy(ft => ft.Category)
                .ToDictionary(g => g.Key, g => g.Select(ft => ft.DisplayName ?? ft.MimeType).ToList());

            var allowedTypesMessage = string.Join(", ",
                allowedByCategory.Select(kvp => $"{kvp.Key}s ({string.Join(", ", kvp.Value)})"));

            return BadRequest(new { message = $"Invalid file type: {file.ContentType}. Allowed types: {allowedTypesMessage}." });
        }

        // Validate file size based on the file type's configured max size
        var maxSize = matchingFileType.MaxSizeMB * 1024 * 1024;
        if (file.Length > maxSize)
        {
            return BadRequest(new { message = $"File size must be less than {matchingFileType.MaxSizeMB}MB for {matchingFileType.DisplayName ?? matchingFileType.MimeType}." });
        }

        // Determine asset type from file type category if not specified
        var detectedAssetType = assetType ?? matchingFileType.Category;

        var userId = GetCurrentUserId();

        try
        {
            // Create asset record first to get the ID
            var asset = new Asset
            {
                AssetType = detectedAssetType,
                FileName = file.FileName,
                ContentType = file.ContentType,
                FileSize = file.Length,
                StorageUrl = string.Empty, // Will be updated after upload
                StorageType = _storageService.StorageType,
                Category = category,
                SiteKey = siteKey,
                UploadedBy = userId,
                IsPublic = isPublic
            };

            _context.Assets.Add(asset);
            await _context.SaveChangesAsync();

            // Now upload with the asset ID as filename
            var storageUrl = await _storageService.UploadFileAsync(file, asset.Id, siteKey);

            // Update asset with storage URL
            asset.StorageUrl = storageUrl;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Asset {AssetId} ({AssetType}) uploaded by user {UserId}", asset.Id, detectedAssetType, userId);

            return Ok(new AssetUploadResponse
            {
                Success = true,
                AssetId = asset.Id,
                AssetType = asset.AssetType,
                FileName = asset.FileName,
                ContentType = asset.ContentType,
                FileSize = asset.FileSize,
                Url = $"/asset/{asset.Id}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading asset");
            return StatusCode(500, new { message = "Failed to upload asset." });
        }
    }

    /// <summary>
    /// Register an external link as an asset (YouTube, Vimeo, etc.) - supports API key with assets:write scope
    /// </summary>
    [HttpPost("link")]
    [ApiKeyAuthorize(ApiScopes.AssetsWrite, AllowJwt = true)]
    public async Task<ActionResult<AssetUploadResponse>> RegisterLink([FromBody] RegisterLinkRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
        {
            return BadRequest(new { message = "URL is required." });
        }

        // Validate URL format
        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri))
        {
            return BadRequest(new { message = "Invalid URL format." });
        }

        var userId = GetCurrentUserId();

        // Extract info from URL (YouTube, Vimeo, etc.)
        var (title, thumbnailUrl, contentType) = ExtractLinkInfo(request.Url);

        var asset = new Asset
        {
            AssetType = request.AssetType ?? AssetTypes.Link,
            FileName = request.Title ?? title ?? "External Link",
            ContentType = contentType,
            FileSize = 0,
            StorageUrl = string.Empty,
            ExternalUrl = request.Url,
            ThumbnailUrl = request.ThumbnailUrl ?? thumbnailUrl,
            StorageType = "external",
            Category = request.Category,
            SiteKey = request.SiteKey,
            UploadedBy = userId,
            IsPublic = request.IsPublic
        };

        _context.Assets.Add(asset);
        await _context.SaveChangesAsync();

        _logger.LogInformation("External link asset {AssetId} registered by user {UserId}: {Url}", asset.Id, userId, request.Url);

        return Ok(new AssetUploadResponse
        {
            Success = true,
            AssetId = asset.Id,
            AssetType = asset.AssetType,
            FileName = asset.FileName,
            ContentType = asset.ContentType,
            FileSize = 0,
            Url = $"/asset/{asset.Id}",
            ExternalUrl = asset.ExternalUrl,
            ThumbnailUrl = asset.ThumbnailUrl
        });
    }

    /// <summary>
    /// Detect asset type from content type (used for external links)
    /// </summary>
    private static string DetectAssetType(string contentType)
    {
        contentType = contentType.ToLower();
        if (contentType.StartsWith("image/")) return AssetTypes.Image;
        if (contentType.StartsWith("video/")) return AssetTypes.Video;
        if (contentType.StartsWith("audio/")) return AssetTypes.Audio;
        if (contentType.Contains("pdf") || contentType.Contains("document") || contentType.Contains("word")
            || contentType.Contains("markdown") || contentType.Contains("html"))
            return AssetTypes.Document;
        return AssetTypes.Image;
    }

    private static (string? title, string? thumbnailUrl, string contentType) ExtractLinkInfo(string url)
    {
        var uri = new Uri(url);
        var host = uri.Host.ToLower();

        // YouTube
        if (host.Contains("youtube.com") || host.Contains("youtu.be"))
        {
            var videoId = ExtractYouTubeVideoId(url);
            if (!string.IsNullOrEmpty(videoId))
            {
                return (null, $"https://img.youtube.com/vi/{videoId}/hqdefault.jpg", "video/youtube");
            }
        }

        // Vimeo
        if (host.Contains("vimeo.com"))
        {
            return (null, null, "video/vimeo");
        }

        // Default
        return (null, null, "text/html");
    }

    private static string? ExtractYouTubeVideoId(string url)
    {
        var uri = new Uri(url);

        // youtube.com/watch?v=VIDEO_ID
        if (uri.Host.Contains("youtube.com"))
        {
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            return query["v"];
        }

        // youtu.be/VIDEO_ID
        if (uri.Host.Contains("youtu.be"))
        {
            return uri.AbsolutePath.TrimStart('/');
        }

        return null;
    }

    /// <summary>
    /// Get asset file by ID (redirects to external URL for linked assets)
    /// Public assets are accessible anonymously, private assets require JWT authentication
    /// </summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAsset(int id)
    {
        var asset = await _context.Assets.FindAsync(id);
        if (asset == null)
        {
            return NotFound();
        }

        // Public assets are accessible to anyone
        // Private assets require authenticated user
        if (!asset.IsPublic)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }
        }

        // For external links, redirect to the external URL
        if (asset.StorageType == "external" && !string.IsNullOrEmpty(asset.ExternalUrl))
        {
            return Redirect(asset.ExternalUrl);
        }

        // For S3 with public URLs, redirect to the S3 URL
        if (asset.StorageType == "s3" && asset.StorageUrl.StartsWith("https://"))
        {
            return Redirect(asset.StorageUrl);
        }

        // For local storage, serve the file
        var stream = await _storageService.GetFileStreamAsync(asset.StorageUrl);
        if (stream == null)
        {
            return NotFound();
        }

        return File(stream, asset.ContentType, asset.FileName);
    }

    /// <summary>
    /// Get asset metadata by ID
    /// Public assets are accessible anonymously, private assets require authentication
    /// </summary>
    [HttpGet("{id:int}/info")]
    [AllowAnonymous]
    public async Task<ActionResult<AssetInfoResponse>> GetAssetInfo(int id)
    {
        var asset = await _context.Assets.FindAsync(id);
        if (asset == null)
        {
            return NotFound();
        }

        // Check if asset is public or user is authenticated
        if (!asset.IsPublic)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }
        }

        return Ok(new AssetInfoResponse
        {
            Id = asset.Id,
            AssetType = asset.AssetType,
            FileName = asset.FileName,
            ContentType = asset.ContentType,
            FileSize = asset.FileSize,
            Category = asset.Category,
            ExternalUrl = asset.ExternalUrl,
            ThumbnailUrl = asset.ThumbnailUrl,
            IsPublic = asset.IsPublic,
            CreatedAt = asset.CreatedAt,
            Url = $"/asset/{asset.Id}"
        });
    }

    /// <summary>
    /// Delete an asset (owner or admin only) - supports API key with assets:write scope
    /// </summary>
    [HttpDelete("{id:int}")]
    [ApiKeyAuthorize(ApiScopes.AssetsWrite, AllowJwt = true)]
    public async Task<ActionResult> DeleteAsset(int id)
    {
        var asset = await _context.Assets.FindAsync(id);
        if (asset == null)
        {
            return NotFound();
        }

        var userId = GetCurrentUserId();
        var isAdmin = User.IsInRole("SU");

        // Only owner or admin can delete
        if (asset.UploadedBy != userId && !isAdmin)
        {
            return Forbid();
        }

        try
        {
            // Delete from storage
            await _storageService.DeleteFileAsync(asset.StorageUrl);

            // Delete record
            _context.Assets.Remove(asset);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Asset {AssetId} deleted by user {UserId}", id, userId);

            return Ok(new { message = "Asset deleted successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting asset {AssetId}", id);
            return StatusCode(500, new { message = "Failed to delete asset." });
        }
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}

#region DTOs

public class AssetUploadResponse
{
    public bool Success { get; set; }
    public int AssetId { get; set; }
    public string AssetType { get; set; } = "image";
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? ExternalUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
}

public class RegisterLinkRequest
{
    public string Url { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? AssetType { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? Category { get; set; }
    public string? SiteKey { get; set; }
    public bool IsPublic { get; set; } = true;
}

public class AssetInfoResponse
{
    public int Id { get; set; }
    public string AssetType { get; set; } = "image";
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? Category { get; set; }
    public string? ExternalUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public bool IsPublic { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Url { get; set; } = string.Empty;
}

#endregion

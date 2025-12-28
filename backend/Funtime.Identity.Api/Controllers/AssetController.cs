using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
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

    public AssetController(
        ApplicationDbContext context,
        IFileStorageService storageService,
        ILogger<AssetController> logger)
    {
        _context = context;
        _storageService = storageService;
        _logger = logger;
    }

    /// <summary>
    /// Upload an asset and get back the asset ID
    /// </summary>
    [HttpPost("upload")]
    [Authorize]
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

        // Validate file size (10MB max)
        if (file.Length > 10 * 1024 * 1024)
        {
            return BadRequest(new { message = "File size must be less than 10MB." });
        }

        // Validate file type
        var allowedImageTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp", "image/svg+xml" };
        var allowedDocTypes = new[] { "application/pdf", "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" };
        var allowedVideoTypes = new[] { "video/mp4", "video/webm", "video/ogg" };
        var allowedAudioTypes = new[] { "audio/mpeg", "audio/wav", "audio/ogg", "audio/mp3" };
        var allAllowedTypes = allowedImageTypes.Concat(allowedDocTypes).Concat(allowedVideoTypes).Concat(allowedAudioTypes).ToArray();

        if (!allAllowedTypes.Contains(file.ContentType.ToLower()))
        {
            return BadRequest(new { message = "Invalid file type." });
        }

        // Determine asset type from content type if not specified
        var detectedAssetType = assetType ?? DetectAssetType(file.ContentType);

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
    /// Register an external link as an asset (YouTube, Vimeo, etc.)
    /// </summary>
    [HttpPost("link")]
    [Authorize]
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

    private static string DetectAssetType(string contentType)
    {
        if (contentType.StartsWith("image/")) return AssetTypes.Image;
        if (contentType.StartsWith("video/")) return AssetTypes.Video;
        if (contentType.StartsWith("audio/")) return AssetTypes.Audio;
        if (contentType.Contains("pdf") || contentType.Contains("document") || contentType.Contains("word"))
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

        // Check if asset is public or user is authenticated
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
    /// Delete an asset (owner or admin only)
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize]
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

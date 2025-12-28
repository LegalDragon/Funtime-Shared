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

        // Validate file type for images
        var allowedImageTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp", "image/svg+xml" };
        var allowedDocTypes = new[] { "application/pdf", "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" };
        var allAllowedTypes = allowedImageTypes.Concat(allowedDocTypes).ToArray();

        if (!allAllowedTypes.Contains(file.ContentType.ToLower()))
        {
            return BadRequest(new { message = "Invalid file type." });
        }

        var userId = GetCurrentUserId();
        var containerName = category ?? "general";

        try
        {
            // Upload to storage (with site organization)
            var storageUrl = await _storageService.UploadFileAsync(file, containerName, siteKey);

            // Create asset record
            var asset = new Asset
            {
                FileName = file.FileName,
                ContentType = file.ContentType,
                FileSize = file.Length,
                StorageUrl = storageUrl,
                StorageType = _storageService.StorageType,
                Category = category,
                SiteKey = siteKey,
                UploadedBy = userId,
                IsPublic = isPublic
            };

            _context.Assets.Add(asset);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Asset {AssetId} uploaded by user {UserId}", asset.Id, userId);

            return Ok(new AssetUploadResponse
            {
                Success = true,
                AssetId = asset.Id,
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
    /// Get asset file by ID
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
            FileName = asset.FileName,
            ContentType = asset.ContentType,
            FileSize = asset.FileSize,
            Category = asset.Category,
            IsPublic = asset.IsPublic,
            CreatedAt = asset.CreatedAt,
            Url = asset.StorageType == "s3" ? asset.StorageUrl : $"/asset/{asset.Id}"
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
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string Url { get; set; } = string.Empty;
}

public class AssetInfoResponse
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? Category { get; set; }
    public bool IsPublic { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Url { get; set; } = string.Empty;
}

#endregion

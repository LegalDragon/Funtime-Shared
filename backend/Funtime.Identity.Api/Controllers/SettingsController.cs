using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Funtime.Identity.Api.Data;
using Funtime.Identity.Api.Models;
using Funtime.Identity.Api.Services;

namespace Funtime.Identity.Api.Controllers;

[ApiController]
[Route("settings")]
public class SettingsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _storageService;
    private readonly ILogger<SettingsController> _logger;

    private const string MainLogoCategory = "system";
    private const string MainLogoSiteKey = "main-logo";

    public SettingsController(
        ApplicationDbContext context,
        IFileStorageService storageService,
        ILogger<SettingsController> logger)
    {
        _context = context;
        _storageService = storageService;
        _logger = logger;
    }

    /// <summary>
    /// Get the main site logo (public endpoint)
    /// </summary>
    [HttpGet("logo")]
    [AllowAnonymous]
    public async Task<ActionResult<MainLogoResponse>> GetMainLogo()
    {
        var logo = await _context.Assets
            .Where(a => a.Category == MainLogoCategory && a.SiteKey == MainLogoSiteKey)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync();

        if (logo == null)
        {
            return Ok(new MainLogoResponse { HasLogo = false });
        }

        return Ok(new MainLogoResponse
        {
            HasLogo = true,
            LogoUrl = $"/asset/{logo.Id}",
            FileName = logo.FileName
        });
    }

    /// <summary>
    /// Upload a new main site logo (admin only)
    /// </summary>
    [HttpPost("logo")]
    [Authorize(Roles = "SU")]
    public async Task<ActionResult<MainLogoResponse>> UploadMainLogo(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file uploaded." });
        }

        // Validate file type
        var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp", "image/svg+xml" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
        {
            return BadRequest(new { message = "Invalid file type. Allowed: JPEG, PNG, GIF, WebP, SVG" });
        }

        // Limit file size to 2MB
        if (file.Length > 2 * 1024 * 1024)
        {
            return BadRequest(new { message = "File size must be less than 2MB." });
        }

        // Delete old logo if exists
        var oldLogo = await _context.Assets
            .Where(a => a.Category == MainLogoCategory && a.SiteKey == MainLogoSiteKey)
            .FirstOrDefaultAsync();

        if (oldLogo != null)
        {
            try
            {
                await _storageService.DeleteFileAsync(oldLogo.StorageUrl);
                _context.Assets.Remove(oldLogo);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete old main logo");
            }
        }

        // Create new asset record
        var asset = new Asset
        {
            AssetType = AssetTypes.Image,
            FileName = file.FileName,
            ContentType = file.ContentType,
            FileSize = file.Length,
            StorageUrl = string.Empty,
            StorageType = _storageService.StorageType,
            Category = MainLogoCategory,
            SiteKey = MainLogoSiteKey,
            IsPublic = true
        };

        _context.Assets.Add(asset);
        await _context.SaveChangesAsync();

        // Upload with asset ID
        var storageUrl = await _storageService.UploadFileAsync(file, asset.Id, MainLogoSiteKey);
        asset.StorageUrl = storageUrl;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Main logo uploaded: Asset {AssetId}", asset.Id);

        return Ok(new MainLogoResponse
        {
            HasLogo = true,
            LogoUrl = $"/asset/{asset.Id}",
            FileName = asset.FileName
        });
    }

    /// <summary>
    /// Delete the main site logo (admin only)
    /// </summary>
    [HttpDelete("logo")]
    [Authorize(Roles = "SU")]
    public async Task<ActionResult> DeleteMainLogo()
    {
        var logo = await _context.Assets
            .Where(a => a.Category == MainLogoCategory && a.SiteKey == MainLogoSiteKey)
            .FirstOrDefaultAsync();

        if (logo == null)
        {
            return NotFound(new { message = "No main logo found." });
        }

        try
        {
            await _storageService.DeleteFileAsync(logo.StorageUrl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete main logo file");
        }

        _context.Assets.Remove(logo);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Main logo deleted");

        return Ok(new { message = "Main logo deleted successfully." });
    }

    /// <summary>
    /// Get logo URL by site key (public endpoint)
    /// If site key is blank or not provided, returns the main logo URL.
    /// If site key is provided, returns that site's logo URL.
    /// Strips "pickleball." prefix if present.
    /// </summary>
    [HttpGet("logo-url")]
    [AllowAnonymous]
    public async Task<ActionResult<LogoUrlResponse>> GetLogoUrl([FromQuery] string? site)
    {
        // Strip "pickleball." prefix if present
        var siteKey = site;
        if (!string.IsNullOrEmpty(siteKey) && siteKey.StartsWith("pickleball.", StringComparison.OrdinalIgnoreCase))
        {
            siteKey = siteKey.Substring("pickleball.".Length);
        }

        // If no site key, return main logo
        if (string.IsNullOrWhiteSpace(siteKey))
        {
            var mainLogo = await _context.Assets
                .Where(a => a.Category == MainLogoCategory && a.SiteKey == MainLogoSiteKey)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            return Ok(new LogoUrlResponse
            {
                LogoUrl = mainLogo != null ? $"/asset/{mainLogo.Id}" : null
            });
        }

        // Look up site by key (case-insensitive)
        var siteRecord = await _context.Sites
            .FirstOrDefaultAsync(s => s.Key.ToLower() == siteKey.ToLower());

        if (siteRecord == null)
        {
            return Ok(new LogoUrlResponse { LogoUrl = null });
        }

        return Ok(new LogoUrlResponse
        {
            LogoUrl = NormalizeAssetUrl(siteRecord.LogoUrl)
        });
    }

    /// <summary>
    /// Get both main logo and site logo for overlay display (public endpoint)
    /// Returns mainLogoUrl and siteLogoUrl based on site key.
    /// Strips "pickleball." prefix if present.
    /// </summary>
    [HttpGet("logo-overlay")]
    [AllowAnonymous]
    public async Task<ActionResult<LogoOverlayResponse>> GetLogoOverlay([FromQuery] string? site)
    {
        // Strip "pickleball." prefix if present
        var siteKey = site;
        if (!string.IsNullOrEmpty(siteKey) && siteKey.StartsWith("pickleball.", StringComparison.OrdinalIgnoreCase))
        {
            siteKey = siteKey.Substring("pickleball.".Length);
        }

        // Get main logo
        var mainLogo = await _context.Assets
            .Where(a => a.Category == MainLogoCategory && a.SiteKey == MainLogoSiteKey)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync();

        var response = new LogoOverlayResponse
        {
            MainLogoUrl = mainLogo != null ? $"/asset/{mainLogo.Id}" : null
        };

        // Get site logo if site key provided
        if (!string.IsNullOrWhiteSpace(siteKey))
        {
            var siteRecord = await _context.Sites
                .FirstOrDefaultAsync(s => s.Key.ToLower() == siteKey.ToLower());

            if (siteRecord != null)
            {
                response.SiteLogoUrl = NormalizeAssetUrl(siteRecord.LogoUrl);
                response.SiteName = siteRecord.Name;
            }
        }

        return Ok(response);
    }

    /// <summary>
    /// Get HTML element for logo overlay display (public endpoint)
    /// Returns ready-to-use HTML with main logo and site logo overlay.
    /// Strips "pickleball." prefix if present.
    /// </summary>
    [HttpGet("logo-html")]
    [AllowAnonymous]
    public async Task<ContentResult> GetLogoHtml([FromQuery] string? site, [FromQuery] string? size = "md")
    {
        // Strip "pickleball." prefix if present
        var siteKey = site;
        if (!string.IsNullOrEmpty(siteKey) && siteKey.StartsWith("pickleball.", StringComparison.OrdinalIgnoreCase))
        {
            siteKey = siteKey.Substring("pickleball.".Length);
        }

        // Get main logo
        var mainLogo = await _context.Assets
            .Where(a => a.Category == MainLogoCategory && a.SiteKey == MainLogoSiteKey)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync();

        string? mainLogoUrl = mainLogo != null ? $"/asset/{mainLogo.Id}" : null;
        string? siteLogoUrl = null;
        string siteName = "Site";

        // Get site logo if site key provided
        if (!string.IsNullOrWhiteSpace(siteKey))
        {
            var siteRecord = await _context.Sites
                .FirstOrDefaultAsync(s => s.Key.ToLower() == siteKey.ToLower());

            if (siteRecord != null)
            {
                // Normalize logo URL to relative path (strip any host prefix)
                siteLogoUrl = NormalizeAssetUrl(siteRecord.LogoUrl);
                siteName = siteRecord.Name;
            }
        }

        // Size classes
        var (containerSize, overlaySize) = size?.ToLower() switch
        {
            "sm" => ("height:2rem", "height:1rem;width:1rem"),
            "lg" => ("height:3.5rem", "height:1.75rem;width:1.75rem"),
            "xl" => ("height:5rem", "height:2.5rem;width:2.5rem"),
            _ => ("height:2.5rem", "height:1.25rem;width:1.25rem") // md default
        };

        // Build HTML
        string html;
        if (mainLogoUrl == null && siteLogoUrl == null)
        {
            // Fallback - no logos
            html = $@"<div style=""display:inline-flex;align-items:center;justify-content:center;{containerSize};aspect-ratio:1;background:linear-gradient(135deg,#3b82f6,#2563eb);border-radius:0.5rem"">
  <span style=""color:white;font-weight:bold;font-size:1.25rem"">{siteName[0]}</span>
</div>";
        }
        else if (mainLogoUrl != null && siteLogoUrl != null)
        {
            // Both logos - overlay effect
            html = $@"<div style=""position:relative;display:inline-block;{containerSize}"">
  <img src=""{mainLogoUrl}"" alt=""Main logo"" style=""width:100%;height:100%;object-fit:contain"" />
  <img src=""{siteLogoUrl}"" alt=""{siteName} logo"" style=""position:absolute;bottom:0;right:0;{overlaySize};object-fit:contain"" />
</div>";
        }
        else if (mainLogoUrl != null)
        {
            // Main logo only
            html = $@"<div style=""display:inline-block;{containerSize}"">
  <img src=""{mainLogoUrl}"" alt=""Main logo"" style=""width:100%;height:100%;object-fit:contain"" />
</div>";
        }
        else
        {
            // Site logo only
            html = $@"<div style=""display:inline-block;{containerSize}"">
  <img src=""{siteLogoUrl}"" alt=""{siteName} logo"" style=""width:100%;height:100%;object-fit:contain"" />
</div>";
        }

        return Content(html, "text/html");
    }

    /// <summary>
    /// Get Terms of Service content (public endpoint)
    /// </summary>
    [HttpGet("terms-of-service")]
    [AllowAnonymous]
    public async Task<ActionResult<LegalContentResponse>> GetTermsOfService()
    {
        var setting = await _context.Settings
            .FirstOrDefaultAsync(s => s.Key == SettingKeys.TermsOfService);

        return Ok(new LegalContentResponse
        {
            Content = setting?.Value ?? "",
            UpdatedAt = setting?.UpdatedAt
        });
    }

    /// <summary>
    /// Update Terms of Service content (admin only)
    /// </summary>
    [HttpPut("terms-of-service")]
    [Authorize(Roles = "SU")]
    public async Task<ActionResult<LegalContentResponse>> UpdateTermsOfService([FromBody] LegalContentRequest request)
    {
        var userId = GetCurrentUserId();
        var setting = await _context.Settings
            .FirstOrDefaultAsync(s => s.Key == SettingKeys.TermsOfService);

        if (setting == null)
        {
            setting = new Setting
            {
                Key = SettingKeys.TermsOfService,
                Value = request.Content ?? "",
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = userId
            };
            _context.Settings.Add(setting);
        }
        else
        {
            setting.Value = request.Content ?? "";
            setting.UpdatedAt = DateTime.UtcNow;
            setting.UpdatedBy = userId;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Terms of Service updated by user {UserId}", userId);

        return Ok(new LegalContentResponse
        {
            Content = setting.Value,
            UpdatedAt = setting.UpdatedAt
        });
    }

    /// <summary>
    /// Get Privacy Policy content (public endpoint)
    /// </summary>
    [HttpGet("privacy-policy")]
    [AllowAnonymous]
    public async Task<ActionResult<LegalContentResponse>> GetPrivacyPolicy()
    {
        var setting = await _context.Settings
            .FirstOrDefaultAsync(s => s.Key == SettingKeys.PrivacyPolicy);

        return Ok(new LegalContentResponse
        {
            Content = setting?.Value ?? "",
            UpdatedAt = setting?.UpdatedAt
        });
    }

    /// <summary>
    /// Update Privacy Policy content (admin only)
    /// </summary>
    [HttpPut("privacy-policy")]
    [Authorize(Roles = "SU")]
    public async Task<ActionResult<LegalContentResponse>> UpdatePrivacyPolicy([FromBody] LegalContentRequest request)
    {
        var userId = GetCurrentUserId();
        var setting = await _context.Settings
            .FirstOrDefaultAsync(s => s.Key == SettingKeys.PrivacyPolicy);

        if (setting == null)
        {
            setting = new Setting
            {
                Key = SettingKeys.PrivacyPolicy,
                Value = request.Content ?? "",
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = userId
            };
            _context.Settings.Add(setting);
        }
        else
        {
            setting.Value = request.Content ?? "";
            setting.UpdatedAt = DateTime.UtcNow;
            setting.UpdatedBy = userId;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Privacy Policy updated by user {UserId}", userId);

        return Ok(new LegalContentResponse
        {
            Content = setting.Value,
            UpdatedAt = setting.UpdatedAt
        });
    }

    /// <summary>
    /// Get user's role for a specific site. Used by calling sites to check if user is admin.
    /// If userId is not specified, returns the role for the authenticated user.
    /// </summary>
    [HttpGet("user-role")]
    [AllowAnonymous]
    public async Task<ActionResult<UserRoleResponse>> GetUserRole(
        [FromQuery] string? site,
        [FromQuery] int? userId)
    {
        // Get user ID from query or from JWT token
        int targetUserId;
        if (userId.HasValue)
        {
            targetUserId = userId.Value;
        }
        else
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                return Unauthorized(new { message = "User ID required or must be authenticated" });
            }
            targetUserId = currentUserId.Value;
        }

        // Normalize site key (strip "pickleball." prefix if present)
        var siteKey = site;
        if (!string.IsNullOrEmpty(siteKey) && siteKey.StartsWith("pickleball.", StringComparison.OrdinalIgnoreCase))
        {
            siteKey = siteKey.Substring("pickleball.".Length);
        }

        // Get user info
        var user = await _context.Users
            .Include(u => u.UserSites)
            .FirstOrDefaultAsync(u => u.Id == targetUserId);

        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        // Build response
        var response = new UserRoleResponse
        {
            UserId = user.Id,
            Email = user.Email,
            SystemRole = user.SystemRole,
            IsSystemAdmin = user.SystemRole == "SU"
        };

        // Check if user is a system admin (SU) - they have access to ALL sites
        var isSystemAdmin = user.SystemRole == "SU";

        // If site specified, get role for that site
        if (!string.IsNullOrEmpty(siteKey))
        {
            var userSite = user.UserSites
                .FirstOrDefault(us => us.SiteKey.Equals(siteKey, StringComparison.OrdinalIgnoreCase));

            response.SiteKey = siteKey;

            // SU users are effectively admins of all sites
            if (isSystemAdmin)
            {
                response.SiteRole = userSite?.Role ?? "admin"; // Show actual role if exists, otherwise "admin"
                response.IsSiteMember = true; // SU has access to all sites
                response.IsSiteAdmin = true;
            }
            else
            {
                response.SiteRole = userSite?.Role;
                response.IsSiteMember = userSite != null && userSite.IsActive;
                response.IsSiteAdmin = userSite?.Role == "admin";
            }
        }
        else
        {
            // Return all site memberships
            // For SU users, also include all sites they have access to
            if (isSystemAdmin)
            {
                // Get all active sites for SU users
                var allSites = await _context.Sites
                    .Where(s => s.IsActive)
                    .Select(s => s.Key)
                    .ToListAsync();

                response.Sites = allSites.Select(siteKey => {
                    var userSite = user.UserSites.FirstOrDefault(us => us.SiteKey == siteKey);
                    return new UserSiteRoleInfo
                    {
                        SiteKey = siteKey,
                        Role = userSite?.Role ?? "admin",
                        IsAdmin = true
                    };
                }).ToList();
            }
            else
            {
                response.Sites = user.UserSites
                    .Where(us => us.IsActive)
                    .Select(us => new UserSiteRoleInfo
                    {
                        SiteKey = us.SiteKey,
                        Role = us.Role,
                        IsAdmin = us.Role == "admin"
                    })
                    .ToList();
            }
        }

        return Ok(response);
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst("id")?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    /// <summary>
    /// Normalize asset URL to relative path.
    /// Converts "http://localhost:5000/asset/123" to "/asset/123"
    /// </summary>
    private static string? NormalizeAssetUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return null;

        // If already a relative path, return as-is
        if (url.StartsWith("/asset/")) return url;

        // Extract /asset/{id} from full URL
        var assetIndex = url.IndexOf("/asset/", StringComparison.OrdinalIgnoreCase);
        if (assetIndex >= 0)
        {
            return url.Substring(assetIndex);
        }

        // Return original if no /asset/ found
        return url;
    }
}

public class MainLogoResponse
{
    public bool HasLogo { get; set; }
    public string? LogoUrl { get; set; }
    public string? FileName { get; set; }
}

public class LegalContentResponse
{
    public string Content { get; set; } = "";
    public DateTime? UpdatedAt { get; set; }
}

public class LegalContentRequest
{
    public string? Content { get; set; }
}

public class LogoUrlResponse
{
    public string? LogoUrl { get; set; }
}

public class LogoOverlayResponse
{
    public string? MainLogoUrl { get; set; }
    public string? SiteLogoUrl { get; set; }
    public string? SiteName { get; set; }
}

public class UserRoleResponse
{
    public int UserId { get; set; }
    public string? Email { get; set; }
    public string? SystemRole { get; set; }
    public bool IsSystemAdmin { get; set; } // True if user is SU (has access to all sites)
    public string? SiteKey { get; set; }
    public string? SiteRole { get; set; }
    public bool? IsSiteMember { get; set; }
    public bool? IsSiteAdmin { get; set; }
    public List<UserSiteRoleInfo>? Sites { get; set; }
}

public class UserSiteRoleInfo
{
    public string SiteKey { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
}

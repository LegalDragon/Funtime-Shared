using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Funtime.Identity.Api.Auth;
using Funtime.Identity.Api.Data;
using Funtime.Identity.Api.DTOs;
using Funtime.Identity.Api.Models;

namespace Funtime.Identity.Api.Controllers;

[ApiController]
[Route("sites")]
public class SitesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SitesController> _logger;

    // Valid site keys
    private static readonly HashSet<string> ValidSiteKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "community",
        "college",
        "date",
        "jobs"
    };

    public SitesController(
        ApplicationDbContext context,
        ILogger<SitesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get sites the current user has joined (supports API key with sites:read scope)
    /// </summary>
    [HttpGet]
    [ApiKeyAuthorize(ApiScopes.SitesRead, AllowJwt = true)]
    public async Task<ActionResult<List<UserSiteResponse>>> GetMySites()
    {
        var userId = GetUserIdFromToken();
        if (userId == null)
        {
            return Unauthorized(new ApiResponse { Success = false, Message = "Invalid token." });
        }

        var sites = await _context.UserSites
            .Where(s => s.UserId == userId)
            .OrderBy(s => s.JoinedAt)
            .ToListAsync();

        return Ok(sites.Select(s => new UserSiteResponse
        {
            Id = s.Id,
            SiteKey = s.SiteKey,
            JoinedAt = s.JoinedAt,
            IsActive = s.IsActive,
            Role = s.Role
        }).ToList());
    }

    /// <summary>
    /// Join a site (supports API key with sites:read scope)
    /// </summary>
    [HttpPost("join")]
    [ApiKeyAuthorize(ApiScopes.SitesRead, AllowJwt = true)]
    public async Task<ActionResult<UserSiteResponse>> JoinSite([FromBody] JoinSiteRequest request)
    {
        var userId = GetUserIdFromToken();
        if (userId == null)
        {
            return Unauthorized(new ApiResponse { Success = false, Message = "Invalid token." });
        }

        var siteKey = request.SiteKey.ToLower();

        if (!ValidSiteKeys.Contains(siteKey))
        {
            return BadRequest(new ApiResponse { Success = false, Message = $"Invalid site key. Valid sites: {string.Join(", ", ValidSiteKeys)}" });
        }

        // Check if already joined
        var existingSite = await _context.UserSites
            .FirstOrDefaultAsync(s => s.UserId == userId && s.SiteKey == siteKey);

        if (existingSite != null)
        {
            if (existingSite.IsActive)
            {
                return BadRequest(new ApiResponse { Success = false, Message = "Already a member of this site." });
            }

            // Reactivate
            existingSite.IsActive = true;
            await _context.SaveChangesAsync();

            return Ok(new UserSiteResponse
            {
                Id = existingSite.Id,
                SiteKey = existingSite.SiteKey,
                JoinedAt = existingSite.JoinedAt,
                IsActive = existingSite.IsActive,
                Role = existingSite.Role
            });
        }

        var userSite = new UserSite
        {
            UserId = userId.Value,
            SiteKey = siteKey,
            JoinedAt = DateTime.UtcNow,
            IsActive = true,
            Role = "member"
        };

        _context.UserSites.Add(userSite);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} joined site {SiteKey}", userId, siteKey);

        return Ok(new UserSiteResponse
        {
            Id = userSite.Id,
            SiteKey = userSite.SiteKey,
            JoinedAt = userSite.JoinedAt,
            IsActive = userSite.IsActive,
            Role = userSite.Role
        });
    }

    /// <summary>
    /// Leave a site (supports API key with sites:read scope)
    /// </summary>
    [HttpPost("leave")]
    [ApiKeyAuthorize(ApiScopes.SitesRead, AllowJwt = true)]
    public async Task<ActionResult<ApiResponse>> LeaveSite([FromBody] JoinSiteRequest request)
    {
        var userId = GetUserIdFromToken();
        if (userId == null)
        {
            return Unauthorized(new ApiResponse { Success = false, Message = "Invalid token." });
        }

        var siteKey = request.SiteKey.ToLower();

        var userSite = await _context.UserSites
            .FirstOrDefaultAsync(s => s.UserId == userId && s.SiteKey == siteKey);

        if (userSite == null)
        {
            return NotFound(new ApiResponse { Success = false, Message = "Not a member of this site." });
        }

        userSite.IsActive = false;
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} left site {SiteKey}", userId, siteKey);

        return Ok(new ApiResponse { Success = true, Message = $"Left {siteKey} successfully." });
    }

    /// <summary>
    /// Update a user's role on a site (requires API key with users:write scope)
    /// </summary>
    [HttpPost("role")]
    [ApiKeyAuthorize(ApiScopes.UsersWrite)]
    public async Task<ActionResult<UserSiteResponse>> UpdateUserRole([FromBody] UpdateSiteRoleRequest request)
    {
        var userId = GetUserIdFromToken();
        if (userId == null)
        {
            return Unauthorized(new ApiResponse { Success = false, Message = "Invalid token." });
        }

        var siteKey = request.SiteKey.ToLower();

        var userSite = await _context.UserSites
            .FirstOrDefaultAsync(s => s.UserId == userId && s.SiteKey == siteKey);

        if (userSite == null)
        {
            return NotFound(new ApiResponse { Success = false, Message = "User is not a member of this site." });
        }

        userSite.Role = request.Role.ToLower();
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} role updated to {Role} on site {SiteKey}", userId, request.Role, siteKey);

        return Ok(new UserSiteResponse
        {
            Id = userSite.Id,
            SiteKey = userSite.SiteKey,
            JoinedAt = userSite.JoinedAt,
            IsActive = userSite.IsActive,
            Role = userSite.Role
        });
    }

    /// <summary>
    /// Check if current user is a member of a specific site (supports API key with sites:read scope)
    /// </summary>
    [HttpGet("check/{siteKey}")]
    [ApiKeyAuthorize(ApiScopes.SitesRead, AllowJwt = true)]
    public async Task<ActionResult<UserSiteResponse?>> CheckMembership(string siteKey)
    {
        var userId = GetUserIdFromToken();
        if (userId == null)
        {
            return Unauthorized(new ApiResponse { Success = false, Message = "Invalid token." });
        }

        var userSite = await _context.UserSites
            .FirstOrDefaultAsync(s => s.UserId == userId && s.SiteKey == siteKey.ToLower() && s.IsActive);

        if (userSite == null)
        {
            return Ok(null);
        }

        return Ok(new UserSiteResponse
        {
            Id = userSite.Id,
            SiteKey = userSite.SiteKey,
            JoinedAt = userSite.JoinedAt,
            IsActive = userSite.IsActive,
            Role = userSite.Role
        });
    }

    private int? GetUserIdFromToken()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}

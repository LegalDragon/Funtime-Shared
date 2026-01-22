using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Funtime.Identity.Api.Auth;
using Funtime.Identity.Api.Data;
using Funtime.Identity.Api.DTOs;
using Funtime.Identity.Api.Models;

namespace Funtime.Identity.Api.Controllers;

[ApiController]
[Route("profile")]
public class ProfileController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(
        ApplicationDbContext context,
        ILogger<ProfileController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get current user's profile (supports API key with users:read scope)
    /// </summary>
    [HttpGet]
    [ApiKeyAuthorize(ApiScopes.UsersRead, AllowJwt = true)]
    public async Task<ActionResult<UserProfileResponse>> GetProfile()
    {
        var userId = GetUserIdFromToken();
        if (userId == null)
        {
            return Unauthorized(new ApiResponse { Success = false, Message = "Invalid token." });
        }

        var profile = await _context.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
        {
            // Return empty profile response
            return Ok(new UserProfileResponse
            {
                UserId = userId.Value,
                CreatedAt = DateTime.UtcNow
            });
        }

        return Ok(MapToProfileResponse(profile));
    }

    /// <summary>
    /// Update current user's profile (supports API key with users:write scope)
    /// </summary>
    [HttpPut]
    [ApiKeyAuthorize(ApiScopes.UsersWrite, AllowJwt = true)]
    public async Task<ActionResult<UserProfileResponse>> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = GetUserIdFromToken();
        if (userId == null)
        {
            return Unauthorized(new ApiResponse { Success = false, Message = "Invalid token." });
        }

        var profile = await _context.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
        {
            // Create new profile
            profile = new UserProfile
            {
                UserId = userId.Value,
                CreatedAt = DateTime.UtcNow
            };
            _context.UserProfiles.Add(profile);
        }

        // Update fields
        profile.FirstName = request.FirstName ?? profile.FirstName;
        profile.LastName = request.LastName ?? profile.LastName;
        profile.DisplayName = request.DisplayName ?? profile.DisplayName;
        profile.AvatarUrl = request.AvatarUrl ?? profile.AvatarUrl;
        profile.City = request.City ?? profile.City;
        profile.State = request.State ?? profile.State;
        profile.Country = request.Country ?? profile.Country;
        profile.SkillLevel = request.SkillLevel ?? profile.SkillLevel;
        profile.Bio = request.Bio ?? profile.Bio;
        profile.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Profile updated for user {UserId}", userId);

        return Ok(MapToProfileResponse(profile));
    }

    /// <summary>
    /// Get profile by user ID (supports API key with users:read scope)
    /// </summary>
    [HttpGet("{userId:int}")]
    [ApiKeyAuthorize(ApiScopes.UsersRead, AllowJwt = true)]
    public async Task<ActionResult<UserProfileResponse>> GetProfileById(int userId)
    {
        var profile = await _context.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
        {
            return NotFound(new ApiResponse { Success = false, Message = "Profile not found." });
        }

        return Ok(MapToProfileResponse(profile));
    }

    /// <summary>
    /// Get full user info including profile and sites (supports API key with users:read scope)
    /// </summary>
    [HttpGet("full")]
    [ApiKeyAuthorize(ApiScopes.UsersRead, AllowJwt = true)]
    public async Task<ActionResult<UserFullResponse>> GetFullProfile()
    {
        var userId = GetUserIdFromToken();
        if (userId == null)
        {
            return Unauthorized(new ApiResponse { Success = false, Message = "Invalid token." });
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(new ApiResponse { Success = false, Message = "User not found." });
        }

        var profile = await _context.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        var sites = await _context.UserSites
            .Where(s => s.UserId == userId)
            .ToListAsync();

        return Ok(new UserFullResponse
        {
            Id = user.Id,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            IsEmailVerified = user.IsEmailVerified,
            IsPhoneVerified = user.IsPhoneVerified,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            Profile = profile != null ? MapToProfileResponse(profile) : null,
            Sites = sites.Select(s => new UserSiteResponse
            {
                Id = s.Id,
                SiteKey = s.SiteKey,
                JoinedAt = s.JoinedAt,
                IsActive = s.IsActive,
                Role = s.Role
            }).ToList(),
            SiteKeys = sites.Where(s => s.IsActive).Select(s => s.SiteKey).ToList()
        });
    }

    private int? GetUserIdFromToken()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private static UserProfileResponse MapToProfileResponse(UserProfile profile)
    {
        return new UserProfileResponse
        {
            Id = profile.Id,
            UserId = profile.UserId,
            FirstName = profile.FirstName,
            LastName = profile.LastName,
            DisplayName = profile.DisplayName,
            AvatarUrl = profile.AvatarUrl,
            City = profile.City,
            State = profile.State,
            Country = profile.Country,
            SkillLevel = profile.SkillLevel,
            Bio = profile.Bio,
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt
        };
    }
}

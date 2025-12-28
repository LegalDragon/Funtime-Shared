using System.ComponentModel.DataAnnotations;

namespace Funtime.Identity.Api.DTOs;

// Profile DTOs

public class UserProfileResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public decimal? SkillLevel { get; set; }
    public string? Bio { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class UpdateProfileRequest
{
    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    [MaxLength(255)]
    public string? DisplayName { get; set; }

    [MaxLength(500)]
    [Url]
    public string? AvatarUrl { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(50)]
    public string? State { get; set; }

    [MaxLength(50)]
    public string? Country { get; set; }

    [Range(1.0, 6.0)]
    public decimal? SkillLevel { get; set; }

    [MaxLength(1000)]
    public string? Bio { get; set; }
}

// Site membership DTOs

public class UserSiteResponse
{
    public int Id { get; set; }
    public string SiteKey { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
    public bool IsActive { get; set; }
    public string Role { get; set; } = "member";
}

public class JoinSiteRequest
{
    [Required]
    [MaxLength(50)]
    public string SiteKey { get; set; } = string.Empty;
}

public class UpdateSiteRoleRequest
{
    [Required]
    [MaxLength(50)]
    public string SiteKey { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Role { get; set; } = string.Empty;

    [Required]
    public string ApiSecretKey { get; set; } = string.Empty;
}

// Full user info with profile and sites
public class UserFullResponse
{
    public int Id { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsEmailVerified { get; set; }
    public bool IsPhoneVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public UserProfileResponse? Profile { get; set; }
    public List<UserSiteResponse> Sites { get; set; } = new();
    public List<string> SiteKeys { get; set; } = new();
}

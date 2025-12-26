using System.ComponentModel.DataAnnotations;

namespace Funtime.Identity.Api.DTOs;

#region Sites

public class SiteResponse
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Url { get; set; }
    public bool IsActive { get; set; }
    public bool RequiresSubscription { get; set; }
    public long? MonthlyPriceCents { get; set; }
    public long? YearlyPriceCents { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateSiteRequest
{
    [Required]
    [MaxLength(50)]
    public string Key { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(255)]
    public string? Url { get; set; }

    public bool IsActive { get; set; } = true;
    public bool RequiresSubscription { get; set; } = false;
    public long? MonthlyPriceCents { get; set; }
    public long? YearlyPriceCents { get; set; }
    public int DisplayOrder { get; set; } = 0;
}

public class UpdateSiteRequest
{
    [MaxLength(100)]
    public string? Name { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(255)]
    public string? Url { get; set; }

    public bool? IsActive { get; set; }
    public bool? RequiresSubscription { get; set; }
    public long? MonthlyPriceCents { get; set; }
    public long? YearlyPriceCents { get; set; }
    public int? DisplayOrder { get; set; }
}

#endregion

#region Users

public class AdminUserResponse
{
    public int Id { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? SystemRole { get; set; }
    public bool IsEmailVerified { get; set; }
    public bool IsPhoneVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class AdminUserListResponse
{
    public List<AdminUserResponse> Users { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class AdminUserDetailResponse : AdminUserResponse
{
    public DateTime? UpdatedAt { get; set; }
    public List<UserSiteInfo> Sites { get; set; } = new();
    public List<SubscriptionInfo> Subscriptions { get; set; } = new();
    public List<PaymentInfo> RecentPayments { get; set; } = new();
}

public class UserSiteInfo
{
    public string SiteKey { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime JoinedAt { get; set; }
}

public class SubscriptionInfo
{
    public int Id { get; set; }
    public string? SiteKey { get; set; }
    public string? PlanName { get; set; }
    public string Status { get; set; } = string.Empty;
    public long? AmountCents { get; set; }
    public string? Interval { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }
}

public class PaymentInfo
{
    public int Id { get; set; }
    public long AmountCents { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? SiteKey { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UpdateUserRequest
{
    [MaxLength(255)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [MaxLength(10)]
    public string? SystemRole { get; set; }

    public bool? IsEmailVerified { get; set; }
    public bool? IsPhoneVerified { get; set; }
}

#endregion

#region Payments

public class AdminPaymentResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? UserEmail { get; set; }
    public long AmountCents { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? SiteKey { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminPaymentListResponse
{
    public List<AdminPaymentResponse> Payments { get; set; } = new();
    public int TotalCount { get; set; }
    public long TotalAmountCents { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

#endregion

#region Stats

public class AdminStatsResponse
{
    public int TotalUsers { get; set; }
    public int NewUsersToday { get; set; }
    public int NewUsersThisWeek { get; set; }
    public int NewUsersThisMonth { get; set; }
    public int ActiveSubscriptions { get; set; }
    public long RevenueThisMonthCents { get; set; }
    public int TotalSites { get; set; }
    public int ActiveSites { get; set; }
}

#endregion

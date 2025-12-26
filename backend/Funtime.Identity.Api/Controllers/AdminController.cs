using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Funtime.Identity.Api.Data;
using Funtime.Identity.Api.DTOs;
using Funtime.Identity.Api.Models;

namespace Funtime.Identity.Api.Controllers;

[ApiController]
[Route("admin")]
[Authorize(Roles = "SU")]
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AdminController> _logger;

    public AdminController(ApplicationDbContext context, ILogger<AdminController> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Sites

    /// <summary>
    /// Get all sites
    /// </summary>
    [HttpGet("sites")]
    public async Task<ActionResult<List<SiteResponse>>> GetSites()
    {
        var sites = await _context.Sites
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.Name)
            .Select(s => new SiteResponse
            {
                Key = s.Key,
                Name = s.Name,
                Description = s.Description,
                Url = s.Url,
                IsActive = s.IsActive,
                RequiresSubscription = s.RequiresSubscription,
                MonthlyPriceCents = s.MonthlyPriceCents,
                YearlyPriceCents = s.YearlyPriceCents,
                DisplayOrder = s.DisplayOrder,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            })
            .ToListAsync();

        return Ok(sites);
    }

    /// <summary>
    /// Create a new site
    /// </summary>
    [HttpPost("sites")]
    public async Task<ActionResult<SiteResponse>> CreateSite([FromBody] CreateSiteRequest request)
    {
        if (await _context.Sites.AnyAsync(s => s.Key == request.Key))
        {
            return BadRequest(new { message = "A site with this key already exists." });
        }

        var site = new Site
        {
            Key = request.Key,
            Name = request.Name,
            Description = request.Description,
            Url = request.Url,
            IsActive = request.IsActive,
            RequiresSubscription = request.RequiresSubscription,
            MonthlyPriceCents = request.MonthlyPriceCents,
            YearlyPriceCents = request.YearlyPriceCents,
            DisplayOrder = request.DisplayOrder
        };

        _context.Sites.Add(site);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Site {SiteKey} created", site.Key);

        return Ok(new SiteResponse
        {
            Key = site.Key,
            Name = site.Name,
            Description = site.Description,
            Url = site.Url,
            IsActive = site.IsActive,
            RequiresSubscription = site.RequiresSubscription,
            MonthlyPriceCents = site.MonthlyPriceCents,
            YearlyPriceCents = site.YearlyPriceCents,
            DisplayOrder = site.DisplayOrder,
            CreatedAt = site.CreatedAt
        });
    }

    /// <summary>
    /// Update a site
    /// </summary>
    [HttpPut("sites/{key}")]
    public async Task<ActionResult<SiteResponse>> UpdateSite(string key, [FromBody] UpdateSiteRequest request)
    {
        var site = await _context.Sites.FindAsync(key);
        if (site == null)
        {
            return NotFound(new { message = "Site not found." });
        }

        site.Name = request.Name ?? site.Name;
        site.Description = request.Description ?? site.Description;
        site.Url = request.Url ?? site.Url;
        site.IsActive = request.IsActive ?? site.IsActive;
        site.RequiresSubscription = request.RequiresSubscription ?? site.RequiresSubscription;
        site.MonthlyPriceCents = request.MonthlyPriceCents ?? site.MonthlyPriceCents;
        site.YearlyPriceCents = request.YearlyPriceCents ?? site.YearlyPriceCents;
        site.DisplayOrder = request.DisplayOrder ?? site.DisplayOrder;
        site.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Site {SiteKey} updated", site.Key);

        return Ok(new SiteResponse
        {
            Key = site.Key,
            Name = site.Name,
            Description = site.Description,
            Url = site.Url,
            IsActive = site.IsActive,
            RequiresSubscription = site.RequiresSubscription,
            MonthlyPriceCents = site.MonthlyPriceCents,
            YearlyPriceCents = site.YearlyPriceCents,
            DisplayOrder = site.DisplayOrder,
            CreatedAt = site.CreatedAt,
            UpdatedAt = site.UpdatedAt
        });
    }

    #endregion

    #region Users

    /// <summary>
    /// Search users
    /// </summary>
    [HttpGet("users")]
    public async Task<ActionResult<AdminUserListResponse>> SearchUsers(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(u =>
                (u.Email != null && u.Email.ToLower().Contains(searchLower)) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(search)));
        }

        var totalCount = await query.CountAsync();

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new AdminUserResponse
            {
                Id = u.Id,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                SystemRole = u.SystemRole,
                IsEmailVerified = u.IsEmailVerified,
                IsPhoneVerified = u.IsPhoneVerified,
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt
            })
            .ToListAsync();

        return Ok(new AdminUserListResponse
        {
            Users = users,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        });
    }

    /// <summary>
    /// Get user details
    /// </summary>
    [HttpGet("users/{id}")]
    public async Task<ActionResult<AdminUserDetailResponse>> GetUser(int id)
    {
        var user = await _context.Users
            .Include(u => u.UserSites)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        // Get payment info
        var paymentCustomer = await _context.PaymentCustomers
            .Include(pc => pc.Subscriptions)
            .Include(pc => pc.Payments.OrderByDescending(p => p.CreatedAt).Take(10))
            .FirstOrDefaultAsync(pc => pc.UserId == id);

        return Ok(new AdminUserDetailResponse
        {
            Id = user.Id,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            SystemRole = user.SystemRole,
            IsEmailVerified = user.IsEmailVerified,
            IsPhoneVerified = user.IsPhoneVerified,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            LastLoginAt = user.LastLoginAt,
            Sites = user.UserSites.Select(us => new UserSiteInfo
            {
                SiteKey = us.SiteKey,
                Role = us.Role,
                IsActive = us.IsActive,
                JoinedAt = us.JoinedAt
            }).ToList(),
            Subscriptions = paymentCustomer?.Subscriptions.Select(s => new SubscriptionInfo
            {
                Id = s.Id,
                SiteKey = s.SiteKey,
                PlanName = s.PlanName,
                Status = s.Status,
                AmountCents = s.AmountCents,
                Interval = s.Interval,
                CurrentPeriodEnd = s.CurrentPeriodEnd
            }).ToList() ?? new List<SubscriptionInfo>(),
            RecentPayments = paymentCustomer?.Payments.Select(p => new PaymentInfo
            {
                Id = p.Id,
                AmountCents = p.AmountCents,
                Currency = p.Currency,
                Status = p.Status,
                Description = p.Description,
                SiteKey = p.SiteKey,
                CreatedAt = p.CreatedAt
            }).ToList() ?? new List<PaymentInfo>()
        });
    }

    /// <summary>
    /// Update user
    /// </summary>
    [HttpPut("users/{id}")]
    public async Task<ActionResult<AdminUserResponse>> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        if (request.Email != null)
        {
            user.Email = request.Email.ToLower();
        }
        if (request.PhoneNumber != null)
        {
            user.PhoneNumber = request.PhoneNumber;
        }
        if (request.SystemRole != null)
        {
            user.SystemRole = request.SystemRole == "" ? null : request.SystemRole;
        }
        if (request.IsEmailVerified.HasValue)
        {
            user.IsEmailVerified = request.IsEmailVerified.Value;
        }
        if (request.IsPhoneVerified.HasValue)
        {
            user.IsPhoneVerified = request.IsPhoneVerified.Value;
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} updated by admin", user.Id);

        return Ok(new AdminUserResponse
        {
            Id = user.Id,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            SystemRole = user.SystemRole,
            IsEmailVerified = user.IsEmailVerified,
            IsPhoneVerified = user.IsPhoneVerified,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        });
    }

    #endregion

    #region Payments

    /// <summary>
    /// Get payments with filters
    /// </summary>
    [HttpGet("payments")]
    public async Task<ActionResult<AdminPaymentListResponse>> GetPayments(
        [FromQuery] int? userId,
        [FromQuery] string? siteKey,
        [FromQuery] string? status,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.Payments
            .Include(p => p.PaymentCustomer)
                .ThenInclude(pc => pc.User)
            .AsQueryable();

        if (userId.HasValue)
        {
            query = query.Where(p => p.PaymentCustomer.UserId == userId.Value);
        }
        if (!string.IsNullOrWhiteSpace(siteKey))
        {
            query = query.Where(p => p.SiteKey == siteKey);
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(p => p.Status == status);
        }
        if (fromDate.HasValue)
        {
            query = query.Where(p => p.CreatedAt >= fromDate.Value);
        }
        if (toDate.HasValue)
        {
            query = query.Where(p => p.CreatedAt <= toDate.Value);
        }

        var totalCount = await query.CountAsync();
        var totalAmount = await query.Where(p => p.Status == "succeeded").SumAsync(p => p.AmountCents);

        var payments = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new AdminPaymentResponse
            {
                Id = p.Id,
                UserId = p.PaymentCustomer.UserId,
                UserEmail = p.PaymentCustomer.User.Email,
                AmountCents = p.AmountCents,
                Currency = p.Currency,
                Status = p.Status,
                Description = p.Description,
                SiteKey = p.SiteKey,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();

        return Ok(new AdminPaymentListResponse
        {
            Payments = payments,
            TotalCount = totalCount,
            TotalAmountCents = totalAmount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        });
    }

    #endregion

    #region Stats

    /// <summary>
    /// Get dashboard stats
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<AdminStatsResponse>> GetStats()
    {
        var totalUsers = await _context.Users.CountAsync();
        var newUsersToday = await _context.Users.CountAsync(u => u.CreatedAt >= DateTime.UtcNow.Date);
        var newUsersThisWeek = await _context.Users.CountAsync(u => u.CreatedAt >= DateTime.UtcNow.AddDays(-7));
        var newUsersThisMonth = await _context.Users.CountAsync(u => u.CreatedAt >= DateTime.UtcNow.AddDays(-30));

        var activeSubscriptions = await _context.Subscriptions.CountAsync(s => s.Status == "active");

        var revenueThisMonth = await _context.Payments
            .Where(p => p.Status == "succeeded" && p.CreatedAt >= DateTime.UtcNow.AddDays(-30))
            .SumAsync(p => p.AmountCents);

        var sitesCount = await _context.Sites.CountAsync();
        var activeSitesCount = await _context.Sites.CountAsync(s => s.IsActive);

        return Ok(new AdminStatsResponse
        {
            TotalUsers = totalUsers,
            NewUsersToday = newUsersToday,
            NewUsersThisWeek = newUsersThisWeek,
            NewUsersThisMonth = newUsersThisMonth,
            ActiveSubscriptions = activeSubscriptions,
            RevenueThisMonthCents = revenueThisMonth,
            TotalSites = sitesCount,
            ActiveSites = activeSitesCount
        });
    }

    #endregion
}

// Navigation property for User -> UserSites
public partial class User
{
    public virtual ICollection<UserSite> UserSites { get; set; } = new List<UserSite>();
}

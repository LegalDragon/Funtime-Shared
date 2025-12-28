using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Funtime.Identity.Api.Data;
using Funtime.Identity.Api.DTOs;
using Funtime.Identity.Api.Models;
using Funtime.Identity.Api.Services;

namespace Funtime.Identity.Api.Controllers;

[ApiController]
[Route("admin")]
[Authorize(Roles = "SU")]
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorageService;
    private readonly IStripeService _stripeService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        ApplicationDbContext context,
        IFileStorageService fileStorageService,
        IStripeService stripeService,
        ILogger<AdminController> logger)
    {
        _context = context;
        _fileStorageService = fileStorageService;
        _stripeService = stripeService;
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
                LogoUrl = s.LogoUrl,
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
            LogoUrl = request.LogoUrl,
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
            LogoUrl = site.LogoUrl,
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
        site.LogoUrl = request.LogoUrl ?? site.LogoUrl;
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
            LogoUrl = site.LogoUrl,
            IsActive = site.IsActive,
            RequiresSubscription = site.RequiresSubscription,
            MonthlyPriceCents = site.MonthlyPriceCents,
            YearlyPriceCents = site.YearlyPriceCents,
            DisplayOrder = site.DisplayOrder,
            CreatedAt = site.CreatedAt,
            UpdatedAt = site.UpdatedAt
        });
    }

    /// <summary>
    /// Upload a logo for a site
    /// </summary>
    [HttpPost("sites/{key}/logo")]
    public async Task<ActionResult<SiteResponse>> UploadSiteLogo(string key, IFormFile file)
    {
        var site = await _context.Sites.FindAsync(key);
        if (site == null)
        {
            return NotFound(new { message = "Site not found." });
        }

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

        // Limit file size to 5MB
        if (file.Length > 5 * 1024 * 1024)
        {
            return BadRequest(new { message = "File size must be less than 5MB." });
        }

        // Delete old file if exists
        if (!string.IsNullOrEmpty(site.LogoUrl))
        {
            try
            {
                await _fileStorageService.DeleteFileAsync(site.LogoUrl);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete old logo for site {SiteKey}", site.Key);
            }
        }

        // Create asset record first to get the ID
        var asset = new Asset
        {
            AssetType = AssetTypes.Image,
            FileName = file.FileName,
            ContentType = file.ContentType,
            FileSize = file.Length,
            StorageUrl = string.Empty,
            StorageType = _fileStorageService.StorageType,
            Category = "logos",
            SiteKey = key,
            IsPublic = true
        };

        _context.Assets.Add(asset);
        await _context.SaveChangesAsync();

        // Upload with asset ID as filename
        var logoUrl = await _fileStorageService.UploadFileAsync(file, asset.Id, key);

        // Update asset with storage URL
        asset.StorageUrl = logoUrl;

        // Update the site's logo URL (use asset endpoint)
        site.LogoUrl = $"/asset/{asset.Id}";
        site.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Logo uploaded for site {SiteKey}", site.Key);

        return Ok(new SiteResponse
        {
            Key = site.Key,
            Name = site.Name,
            Description = site.Description,
            Url = site.Url,
            LogoUrl = site.LogoUrl,
            IsActive = site.IsActive,
            RequiresSubscription = site.RequiresSubscription,
            MonthlyPriceCents = site.MonthlyPriceCents,
            YearlyPriceCents = site.YearlyPriceCents,
            DisplayOrder = site.DisplayOrder,
            CreatedAt = site.CreatedAt,
            UpdatedAt = site.UpdatedAt
        });
    }

    /// <summary>
    /// Delete a site's logo
    /// </summary>
    [HttpDelete("sites/{key}/logo")]
    public async Task<ActionResult<SiteResponse>> DeleteSiteLogo(string key)
    {
        var site = await _context.Sites.FindAsync(key);
        if (site == null)
        {
            return NotFound(new { message = "Site not found." });
        }

        // Delete the file from S3
        if (!string.IsNullOrEmpty(site.LogoUrl))
        {
            try
            {
                await _fileStorageService.DeleteFileAsync(site.LogoUrl);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete logo from storage for site {SiteKey}", site.Key);
            }
        }

        site.LogoUrl = null;
        site.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Logo deleted for site {SiteKey}", site.Key);

        return Ok(new SiteResponse
        {
            Key = site.Key,
            Name = site.Name,
            Description = site.Description,
            Url = site.Url,
            LogoUrl = site.LogoUrl,
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
        [FromQuery] string? siteKey,
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

        if (!string.IsNullOrWhiteSpace(siteKey))
        {
            query = query.Where(u => u.UserSites.Any(us => us.SiteKey == siteKey));
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
        [FromQuery] string? userSearch,
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
        if (!string.IsNullOrWhiteSpace(userSearch))
        {
            var searchLower = userSearch.ToLower();
            query = query.Where(p =>
                (p.PaymentCustomer.User.Email != null && p.PaymentCustomer.User.Email.ToLower().Contains(searchLower)) ||
                (p.PaymentCustomer.User.PhoneNumber != null && p.PaymentCustomer.User.PhoneNumber.Contains(userSearch)));
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

    /// <summary>
    /// Manually charge a user
    /// </summary>
    [HttpPost("payments/charge")]
    public async Task<ActionResult<ManualChargeResponse>> ManualCharge([FromBody] ManualChargeRequest request)
    {
        // Verify user exists
        var user = await _context.Users.FindAsync(request.UserId);
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        try
        {
            // Get or create customer
            var customer = await _stripeService.GetOrCreateCustomerAsync(request.UserId, user.Email);

            // Create payment intent options
            var paymentIntentOptions = new PaymentIntentCreateOptions
            {
                Amount = request.AmountCents,
                Currency = request.Currency.ToLower(),
                Customer = customer.StripeCustomerId,
                Description = request.Description,
                Metadata = new Dictionary<string, string>
                {
                    { "funtime_user_id", request.UserId.ToString() },
                    { "site_key", request.SiteKey ?? "" },
                    { "manual_charge", "true" },
                    { "charged_by", User.Identity?.Name ?? "admin" }
                }
            };

            // If payment method provided, confirm immediately
            if (!string.IsNullOrWhiteSpace(request.PaymentMethodId))
            {
                paymentIntentOptions.PaymentMethod = request.PaymentMethodId;
                paymentIntentOptions.Confirm = true;
                paymentIntentOptions.OffSession = true;
            }

            var paymentIntentService = new PaymentIntentService();
            var paymentIntent = await paymentIntentService.CreateAsync(paymentIntentOptions);

            // Record payment in database
            var payment = new Payment
            {
                PaymentCustomerId = customer.Id,
                StripePaymentId = paymentIntent.Id,
                AmountCents = request.AmountCents,
                Currency = request.Currency.ToLower(),
                Status = paymentIntent.Status,
                Description = request.Description,
                SiteKey = request.SiteKey,
                CreatedAt = DateTime.UtcNow
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Manual charge created for user {UserId}: {Amount} {Currency} - Status: {Status}",
                request.UserId, request.AmountCents, request.Currency, paymentIntent.Status);

            return Ok(new ManualChargeResponse
            {
                PaymentId = payment.Id,
                StripePaymentIntentId = paymentIntent.Id,
                Status = paymentIntent.Status,
                AmountCents = request.AmountCents,
                Currency = request.Currency.ToLower(),
                ClientSecret = paymentIntent.ClientSecret
            });
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to create manual charge for user {UserId}", request.UserId);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get payment methods for a user (admin only)
    /// </summary>
    [HttpGet("users/{userId}/payment-methods")]
    public async Task<ActionResult<List<AdminPaymentMethodResponse>>> GetUserPaymentMethods(int userId)
    {
        var methods = await _stripeService.GetPaymentMethodsAsync(userId);

        return Ok(methods.Select(m => new AdminPaymentMethodResponse
        {
            Id = m.Id,
            StripePaymentMethodId = m.StripePaymentMethodId,
            Type = m.Type,
            CardBrand = m.CardBrand,
            CardLast4 = m.CardLast4,
            CardExpMonth = m.CardExpMonth,
            CardExpYear = m.CardExpYear,
            IsDefault = m.IsDefault,
            CreatedAt = m.CreatedAt
        }).ToList());
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

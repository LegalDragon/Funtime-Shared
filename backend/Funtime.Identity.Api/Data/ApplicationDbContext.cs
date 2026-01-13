using Microsoft.EntityFrameworkCore;
using Funtime.Identity.Api.Models;

namespace Funtime.Identity.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // Auth tables
    public DbSet<User> Users { get; set; }
    public DbSet<OtpRequest> OtpRequests { get; set; }
    public DbSet<OtpRateLimit> OtpRateLimits { get; set; }
    public DbSet<ExternalLogin> ExternalLogins { get; set; }
    public DbSet<CredentialChangeOtp> CredentialChangeOtps { get; set; }

    // Site tables
    public DbSet<Site> Sites { get; set; }

    // Profile tables
    public DbSet<UserProfile> UserProfiles { get; set; }
    public DbSet<UserSite> UserSites { get; set; }

    // Payment tables
    public DbSet<PaymentCustomer> PaymentCustomers { get; set; }
    public DbSet<PaymentMethod> PaymentMethods { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }

    // Asset tables
    public DbSet<Asset> Assets { get; set; }

    // Settings tables
    public DbSet<Setting> Settings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique().HasFilter("[Email] IS NOT NULL");
            entity.HasIndex(e => e.PhoneNumber).IsUnique().HasFilter("[PhoneNumber] IS NOT NULL");
        });

        // OtpRequest configuration
        modelBuilder.Entity<OtpRequest>(entity =>
        {
            entity.HasIndex(e => new { e.Identifier, e.Code });
            entity.HasIndex(e => e.ExpiresAt);
        });

        // OtpRateLimit configuration
        modelBuilder.Entity<OtpRateLimit>(entity =>
        {
            entity.HasIndex(e => e.Identifier).IsUnique();
        });

        // ExternalLogin configuration
        modelBuilder.Entity<ExternalLogin>(entity =>
        {
            entity.HasIndex(e => new { e.Provider, e.ProviderUserId }).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.Provider }).IsUnique();
            entity.HasIndex(e => e.UserId);
        });

        // CredentialChangeOtp configuration
        modelBuilder.Entity<CredentialChangeOtp>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.ChangeType, e.NewValue, e.ExpiresAt });
            entity.HasIndex(e => e.ExpiresAt);
        });

        // UserProfile configuration
        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasIndex(e => e.UserId).IsUnique();
        });

        // UserSite configuration
        modelBuilder.Entity<UserSite>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.SiteKey }).IsUnique();
            entity.HasIndex(e => e.SiteKey);
        });

        // PaymentCustomer configuration
        modelBuilder.Entity<PaymentCustomer>(entity =>
        {
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasIndex(e => e.StripeCustomerId).IsUnique();
        });

        // PaymentMethod configuration
        modelBuilder.Entity<PaymentMethod>(entity =>
        {
            entity.HasIndex(e => e.StripePaymentMethodId).IsUnique();
            entity.HasIndex(e => e.PaymentCustomerId);
        });

        // Payment configuration
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasIndex(e => e.StripePaymentId).IsUnique();
            entity.HasIndex(e => e.PaymentCustomerId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
        });

        // Subscription configuration
        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasIndex(e => e.StripeSubscriptionId).IsUnique();
            entity.HasIndex(e => e.PaymentCustomerId);
            entity.HasIndex(e => e.Status);
        });

        // Asset configuration
        modelBuilder.Entity<Asset>(entity =>
        {
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.UploadedBy);
            entity.HasIndex(e => e.CreatedAt);
        });

        // Setting configuration
        modelBuilder.Entity<Setting>(entity =>
        {
            entity.HasIndex(e => e.Key).IsUnique();
        });
    }
}

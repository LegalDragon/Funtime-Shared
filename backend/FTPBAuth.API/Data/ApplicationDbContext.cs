using Microsoft.EntityFrameworkCore;
using FTPBAuth.API.Models;

namespace FTPBAuth.API.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<OtpRequest> OtpRequests { get; set; }
    public DbSet<OtpRateLimit> OtpRateLimits { get; set; }
    public DbSet<ExternalLogin> ExternalLogins { get; set; }

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
            entity.HasIndex(e => new { e.PhoneNumber, e.Code });
            entity.HasIndex(e => e.ExpiresAt);
        });

        // OtpRateLimit configuration
        modelBuilder.Entity<OtpRateLimit>(entity =>
        {
            entity.HasIndex(e => e.PhoneNumber).IsUnique();
        });

        // ExternalLogin configuration
        modelBuilder.Entity<ExternalLogin>(entity =>
        {
            // Each provider+providerUserId combination must be unique
            entity.HasIndex(e => new { e.Provider, e.ProviderUserId }).IsUnique();
            // Each user can only have one login per provider
            entity.HasIndex(e => new { e.UserId, e.Provider }).IsUnique();
            // Index for looking up by user
            entity.HasIndex(e => e.UserId);
        });
    }
}

using Microsoft.EntityFrameworkCore;
using TalentBridge.Identity.Application.Interfaces;
using TalentBridge.Identity.Domain.Entities;

namespace TalentBridge.Identity.Infrastructure.Persistence;

public class IdentityDbContext : DbContext, IIdentityDbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();

    Task<int> IIdentityDbContext.SaveChangesAsync(CancellationToken ct) => SaveChangesAsync(ct);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Email).IsRequired().HasMaxLength(256);
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.PasswordHash).IsRequired();
            e.Property(u => u.Role).HasConversion<string>();
            e.Property(u => u.FullName).HasMaxLength(200).HasDefaultValue("");
            e.Property(u => u.IsActive).HasDefaultValue(true);
            e.Property(u => u.Phone).HasMaxLength(30);
            e.Property(u => u.Title).HasMaxLength(100);
            e.Property(u => u.Bio).HasMaxLength(2000);
            e.Property(u => u.Skills).HasMaxLength(1000);
            e.Property(u => u.ResumeUrl).HasMaxLength(500);
            e.Property(u => u.LinkedInUrl).HasMaxLength(300);
            e.Property(u => u.GitHubUrl).HasMaxLength(300);
            e.Ignore(u => u.DomainEvents);
        });
    }
}

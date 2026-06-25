using Microsoft.EntityFrameworkCore;
using TalentBridge.Notifications.Application.Interfaces;
using TalentBridge.Notifications.Domain.Entities;

namespace TalentBridge.Notifications.Infrastructure.Persistence;

public class NotificationsDbContext : DbContext, INotificationsDbContext
{
    public NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : base(options) { }

    public DbSet<Notification> Notifications => Set<Notification>();

    Task<int> INotificationsDbContext.SaveChangesAsync(CancellationToken ct) => SaveChangesAsync(ct);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(e =>
        {
            e.HasKey(n => n.Id);
            e.Property(n => n.Message).IsRequired().HasMaxLength(500);
            e.Property(n => n.IsRead).HasDefaultValue(false);
            e.HasIndex(n => n.UserId);
            e.HasIndex(n => new { n.UserId, n.IsRead });
        });
    }
}

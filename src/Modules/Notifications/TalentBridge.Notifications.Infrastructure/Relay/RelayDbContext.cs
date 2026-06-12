using Microsoft.EntityFrameworkCore;
using TalentBridge.Notifications.Infrastructure.Interfaces;
using TalentBridge.Shared.Outbox;

namespace TalentBridge.Notifications.Infrastructure.Relay;

public class RelayDbContext : DbContext, IRelayDbContext
{
    public RelayDbContext(DbContextOptions<RelayDbContext> options) : base(options) { }

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    Task<int> IRelayDbContext.SaveChangesAsync(CancellationToken ct) => SaveChangesAsync(ct);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OutboxMessage>(e =>
        {
            e.HasKey(o => o.Id);
            e.ToTable("ApplicationsOutboxMessages");
        });
    }
}

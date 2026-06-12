using Microsoft.EntityFrameworkCore;
using TalentBridge.Shared.Outbox;

namespace TalentBridge.Notifications.Infrastructure.Interfaces;

public interface IRelayDbContext
{
    DbSet<OutboxMessage> OutboxMessages { get; }
    Task<int> SaveChangesAsync(CancellationToken ct);
}

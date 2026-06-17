using Microsoft.EntityFrameworkCore;
using TalentBridge.Notifications.Infrastructure.Interfaces;
using TalentBridge.Shared.Outbox;

namespace TalentBridge.Notifications.Infrastructure.Relay;

public class OutboxRepository : IOutboxRepository
{
    private readonly IRelayDbContext _dbContext;

    public OutboxRepository(IRelayDbContext dbContext) => _dbContext = dbContext;

    public async Task<List<OutboxMessage>> GetPendingAsync(CancellationToken ct) =>
        await _dbContext.OutboxMessages
            .Where(m => m.ProcessedOnUtc == null)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(50)
            .ToListAsync(ct);

    public async Task SaveAsync(OutboxMessage message, CancellationToken ct)
    {
        _dbContext.OutboxMessages.Update(message);
        await _dbContext.SaveChangesAsync(ct);
    }
}

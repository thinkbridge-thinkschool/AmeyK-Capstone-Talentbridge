using TalentBridge.Shared.Outbox;

namespace TalentBridge.Notifications.Infrastructure.Interfaces;

public interface IOutboxRepository
{
    Task<List<OutboxMessage>> GetPendingAsync(CancellationToken ct);
    Task SaveAsync(OutboxMessage message, CancellationToken ct);
}

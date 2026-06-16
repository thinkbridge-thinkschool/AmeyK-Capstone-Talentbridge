using TalentBridge.Shared.Domain;

namespace TalentBridge.Jobs.Domain.Events;

public record JobClosedEvent(Guid JobId, DateTime OccurredOnUtc) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

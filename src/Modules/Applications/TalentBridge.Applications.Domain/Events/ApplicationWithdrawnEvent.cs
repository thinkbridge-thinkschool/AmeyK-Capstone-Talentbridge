using TalentBridge.Shared.Domain;

namespace TalentBridge.Applications.Domain.Events;

public record ApplicationWithdrawnEvent(Guid ApplicationId, DateTime OccurredOnUtc) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

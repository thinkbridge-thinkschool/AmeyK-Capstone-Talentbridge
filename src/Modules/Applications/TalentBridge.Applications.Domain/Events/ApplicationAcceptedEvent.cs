using TalentBridge.Shared.Domain;

namespace TalentBridge.Applications.Domain.Events;

public record ApplicationAcceptedEvent(Guid ApplicationId, Guid CandidateId, Guid JobId, DateTime OccurredOnUtc) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

using TalentBridge.Shared.Domain;

namespace TalentBridge.Applications.Domain.Events;

public record ApplicationSubmittedEvent(Guid ApplicationId, Guid JobId, Guid CandidateId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

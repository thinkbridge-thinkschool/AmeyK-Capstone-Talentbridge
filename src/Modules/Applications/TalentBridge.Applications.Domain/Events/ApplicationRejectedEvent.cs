using TalentBridge.Shared.Domain;

namespace TalentBridge.Applications.Domain.Events;

public record ApplicationRejectedEvent(Guid ApplicationId, Guid CandidateId, string Reason) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

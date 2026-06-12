using TalentBridge.Applications.Domain.Enums;
using TalentBridge.Shared.Domain;

namespace TalentBridge.Applications.Domain.Events;

public record ApplicationStatusChangedEvent(Guid ApplicationId, Guid CandidateId, ApplicationStatus NewStatus) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

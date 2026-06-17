using TalentBridge.Applications.Domain.Enums;
using TalentBridge.Shared.Domain;

namespace TalentBridge.Applications.Domain.Events;

public record ApplicationStatusChangedEvent(
    Guid ApplicationId, ApplicationStatus OldStatus, ApplicationStatus NewStatus, DateTime OccurredOnUtc) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

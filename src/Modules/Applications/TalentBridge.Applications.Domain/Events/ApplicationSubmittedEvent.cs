using TalentBridge.Shared.Domain;

namespace TalentBridge.Applications.Domain.Events;

public record ApplicationSubmittedEvent(
    Guid ApplicationId, Guid CandidateId, Guid JobId,
    string CoverLetter, string ResumeUrl, DateTime OccurredOnUtc) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

using TalentBridge.Shared.Domain;

namespace TalentBridge.Jobs.Domain.Events;

public record JobPublishedEvent(
    Guid JobId,
    Guid CompanyId,
    string Title,
    string Location,
    List<string> RequiredSkills) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

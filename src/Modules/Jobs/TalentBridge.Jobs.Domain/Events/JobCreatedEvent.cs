using TalentBridge.Shared.Domain;

namespace TalentBridge.Jobs.Domain.Events;

public record JobCreatedEvent(Guid JobId, Guid CompanyId, string Title) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

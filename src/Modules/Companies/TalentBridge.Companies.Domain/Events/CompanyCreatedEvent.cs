using TalentBridge.Shared.Domain;

namespace TalentBridge.Companies.Domain.Events;

public record CompanyCreatedEvent(Guid CompanyId, string Name, Guid OwnerId, DateTime OccurredOnUtc) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

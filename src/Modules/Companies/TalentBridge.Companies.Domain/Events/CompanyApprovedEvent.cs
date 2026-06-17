using TalentBridge.Shared.Domain;

namespace TalentBridge.Companies.Domain.Events;

public record CompanyApprovedEvent(Guid CompanyId, Guid AdminId, DateTime OccurredOnUtc) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

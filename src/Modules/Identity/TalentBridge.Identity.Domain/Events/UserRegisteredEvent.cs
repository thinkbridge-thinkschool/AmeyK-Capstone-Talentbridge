using TalentBridge.Identity.Domain.Enums;
using TalentBridge.Shared.Domain;

namespace TalentBridge.Identity.Domain.Events;

public record UserRegisteredEvent(Guid UserId, string Email, UserRole Role, DateTime OccurredOnUtc) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

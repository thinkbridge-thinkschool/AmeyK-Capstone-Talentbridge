using MediatR;

namespace TalentBridge.Shared.Domain;

public interface IDomainEvent : INotification
{
    Guid EventId { get; }
    DateTime OccurredOnUtc { get; }
}

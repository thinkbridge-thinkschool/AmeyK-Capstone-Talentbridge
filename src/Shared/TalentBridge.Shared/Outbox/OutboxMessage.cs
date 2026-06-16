using System.Text.Json;
using TalentBridge.Shared.Domain;

namespace TalentBridge.Shared.Outbox;

public class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime OccurredOnUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedOnUtc { get; set; }

    public static OutboxMessage Create(IDomainEvent domainEvent) => new()
    {
        Id = Guid.NewGuid(),
        Type = domainEvent.GetType().Name,
        Payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
        OccurredOnUtc = domainEvent.OccurredOnUtc
    };
}

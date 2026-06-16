namespace TalentBridge.Shared.Domain;

public abstract class AggregateRoot<TId> : BaseEntity
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}

// Non-generic alias for Guid-keyed aggregates
public abstract class AggregateRoot : AggregateRoot<Guid> { }

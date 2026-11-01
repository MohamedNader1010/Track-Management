namespace TrackManagement.Domain.Common;

/// <summary>Base type for anything with identity. Collects domain events until the unit of work commits.</summary>
public abstract class Entity<TId> : IHasDomainEvents, IEquatable<Entity<TId>>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected Entity(TId id) => Id = id;

    // Required by EF Core for materialisation.
    protected Entity() => Id = default!;

    public TId Id { get; protected init; }

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public bool Equals(Entity<TId>? other) =>
        other is not null && other.GetType() == GetType() && EqualityComparer<TId>.Default.Equals(Id, other.Id);

    public override bool Equals(object? obj) => obj is Entity<TId> other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}

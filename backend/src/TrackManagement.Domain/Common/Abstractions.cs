using MediatR;

namespace TrackManagement.Domain.Common;

/// <summary>Something that happened in the domain that other parts of the system may react to.</summary>
public interface IDomainEvent : INotification
{
}

public interface IHasDomainEvents
{
    IReadOnlyList<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}

public interface IAuditableEntity
{
    DateTimeOffset CreatedAtUtc { get; }

    DateTimeOffset UpdatedAtUtc { get; }
}

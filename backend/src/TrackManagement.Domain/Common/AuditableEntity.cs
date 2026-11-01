namespace TrackManagement.Domain.Common;

/// <summary>
/// Timestamps are owned by infrastructure: the <c>AuditableEntityInterceptor</c> stamps them on SaveChanges,
/// so no handler can forget (or fake) them.
/// </summary>
public abstract class AuditableEntity<TId> : Entity<TId>, IAuditableEntity
    where TId : notnull
{
    protected AuditableEntity(TId id) : base(id) { }

    protected AuditableEntity() { }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }
}

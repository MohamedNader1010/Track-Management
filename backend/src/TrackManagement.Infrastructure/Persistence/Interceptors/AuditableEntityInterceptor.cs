using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

using TrackManagement.Domain.Common;

namespace TrackManagement.Infrastructure.Persistence.Interceptors;

/// <summary>Stamps CreatedAtUtc / UpdatedAtUtc on every save, using an injectable clock.</summary>
public sealed class AuditableEntityInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        StampAuditFields(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        StampAuditFields(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void StampAuditFields(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = timeProvider.GetUtcNow();

        foreach (var entry in context.ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property(nameof(IAuditableEntity.CreatedAtUtc)).CurrentValue = now;
            }

            if (entry.State is EntityState.Added or EntityState.Modified || HasChangedOwnedChildren(entry))
            {
                entry.Property(nameof(IAuditableEntity.UpdatedAtUtc)).CurrentValue = now;
            }
        }
    }

    // A track whose distributions changed is "updated" even if no column on the track row itself changed.
    private static bool HasChangedOwnedChildren(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry) =>
        entry.Navigations
            .Where(n => n.Metadata.IsCollection)
            .SelectMany(n => n.CurrentValue as IEnumerable<object> ?? Enumerable.Empty<object>())
            .Select(child => entry.Context.Entry(child))
            .Any(child => child.State is EntityState.Added or EntityState.Modified);
}

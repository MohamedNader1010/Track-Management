using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

using TrackManagement.Domain.Common;

namespace TrackManagement.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Publishes domain events through MediatR AFTER the transaction commits, so handlers never react to data
/// that was rolled back. Trade-off: if the process crashes between commit and publish, the event is lost —
/// a transactional outbox would close that gap in production.
/// </summary>
public sealed class DispatchDomainEventsInterceptor(IPublisher publisher) : SaveChangesInterceptor
{
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            await DispatchAsync(eventData.Context, cancellationToken);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        if (eventData.Context is not null)
        {
            DispatchAsync(eventData.Context, CancellationToken.None).GetAwaiter().GetResult();
        }

        return base.SavedChanges(eventData, result);
    }

    private async Task DispatchAsync(DbContext context, CancellationToken cancellationToken)
    {
        var entities = context.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Select(entry => entry.Entity)
            .Where(entity => entity.DomainEvents.Count > 0)
            .ToList();

        var domainEvents = entities.SelectMany(entity => entity.DomainEvents).ToList();

        // Clear first so a handler that saves again can't re-publish the same events.
        entities.ForEach(entity => entity.ClearDomainEvents());

        foreach (var domainEvent in domainEvents)
        {
            await publisher.Publish(domainEvent, cancellationToken);
        }
    }
}

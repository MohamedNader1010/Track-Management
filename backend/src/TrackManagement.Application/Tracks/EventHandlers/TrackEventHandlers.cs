using MediatR;

using Microsoft.Extensions.Logging;

using TrackManagement.Domain.Tracks;

namespace TrackManagement.Application.Tracks.EventHandlers;

/// <summary>
/// Reacts after the track has been committed. In production this is where the delivery job per DSP
/// would be enqueued (ideally via a transactional outbox so it can't be lost if the process dies).
/// </summary>
public sealed class TrackSubmittedToDspsEventHandler(ILogger<TrackSubmittedToDspsEventHandler> logger)
    : INotificationHandler<TrackSubmittedToDspsDomainEvent>
{
    public Task Handle(TrackSubmittedToDspsDomainEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Track {TrackId} queued for delivery to {DspCount} DSP(s): {DspIds}",
            notification.TrackId,
            notification.DspIds.Count,
            notification.DspIds);

        return Task.CompletedTask;
    }
}

public sealed class TrackStatusChangedEventHandler(ILogger<TrackStatusChangedEventHandler> logger)
    : INotificationHandler<TrackStatusChangedDomainEvent>
{
    public Task Handle(TrackStatusChangedDomainEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Track {TrackId} status changed {FromStatus} -> {ToStatus}",
            notification.TrackId,
            notification.FromStatus,
            notification.ToStatus);

        return Task.CompletedTask;
    }
}

public sealed class TrackCreatedEventHandler(ILogger<TrackCreatedEventHandler> logger)
    : INotificationHandler<TrackCreatedDomainEvent>
{
    public Task Handle(TrackCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Track {TrackId} ({Isrc}) created for artist {ArtistId}",
            notification.TrackId,
            notification.Isrc,
            notification.ArtistId);

        return Task.CompletedTask;
    }
}

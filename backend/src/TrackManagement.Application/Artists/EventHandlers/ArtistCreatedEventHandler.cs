using MediatR;

using Microsoft.Extensions.Logging;

using TrackManagement.Domain.Artists;

namespace TrackManagement.Application.Artists.EventHandlers;

/// <summary>Placeholder for onboarding side effects (welcome email, CRM sync...).</summary>
public sealed class ArtistCreatedEventHandler(ILogger<ArtistCreatedEventHandler> logger)
    : INotificationHandler<ArtistCreatedDomainEvent>
{
    public Task Handle(ArtistCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Artist {ArtistId} ({ArtistName}) onboarded — onboarding workflow would start here",
            notification.ArtistId,
            notification.Name);

        return Task.CompletedTask;
    }
}

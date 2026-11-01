using TrackManagement.Domain.Artists;

namespace TrackManagement.Application.Artists;

public sealed record ArtistResponse(Guid Id, string Name, string Email, string Country, DateTimeOffset CreatedAtUtc);

public static class ArtistMappings
{
    public static ArtistResponse ToResponse(this Artist artist) =>
        new(artist.Id, artist.Name, artist.Email, artist.Country, artist.CreatedAtUtc);
}

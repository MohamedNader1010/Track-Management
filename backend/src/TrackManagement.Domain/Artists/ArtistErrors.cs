using ErrorOr;

namespace TrackManagement.Domain.Artists;

public static class ArtistErrors
{
    public static Error NotFound(Guid artistId) =>
        Error.NotFound("Artist.NotFound", $"Artist '{artistId}' was not found.");

    public static Error EmailAlreadyExists(string email) =>
        Error.Conflict("Artist.EmailAlreadyExists", $"An artist with email '{email}' already exists.");
}

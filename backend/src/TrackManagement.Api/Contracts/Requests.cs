namespace TrackManagement.Api.Contracts;

public sealed record CreateArtistRequest(string Name, string Email, string Country);

public sealed record CreateTrackRequest(string Title, Guid ArtistId, string Isrc, DateOnly ReleaseDate, string Genre);

public sealed record DistributeTrackRequest(List<Guid>? DspIds);

public sealed record UpdateTrackStatusRequest(string Status);

public sealed record LoginRequest(string Username, string Password);

public sealed record CurrentUserResponse(string Username, List<string> Roles);

namespace TrackManagement.Application.Tracks;

public sealed record TrackSummaryResponse(
    Guid Id,
    string Title,
    string Isrc,
    Guid ArtistId,
    string ArtistName,
    string Genre,
    string Status,
    DateOnly ReleaseDate,
    int DistributionCount);

public sealed record TrackDetailsResponse(
    Guid Id,
    string Title,
    string Isrc,
    TrackArtistResponse Artist,
    string Genre,
    string Status,
    DateOnly ReleaseDate,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    List<string> AllowedStatusTransitions,
    List<TrackDistributionResponse> Distributions);

public sealed record TrackArtistResponse(Guid Id, string Name, string Country);

public sealed record TrackDistributionResponse(
    Guid Id,
    Guid DspId,
    string DspName,
    string Status,
    DateTimeOffset SubmittedAtUtc);

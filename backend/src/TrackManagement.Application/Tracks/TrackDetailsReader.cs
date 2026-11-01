using Microsoft.EntityFrameworkCore;

using TrackManagement.Application.Common.Interfaces;

namespace TrackManagement.Application.Tracks;

/// <summary>
/// Builds the track details read model. Shared by the GetTrackById query and by commands that return the
/// updated track, so commands don't have to dispatch a query through MediatR from inside a handler.
/// </summary>
internal static class TrackDetailsReader
{
    public static async Task<TrackDetailsResponse?> ReadAsync(
        IApplicationDbContext db,
        Guid trackId,
        CancellationToken cancellationToken)
    {
        var track = await db.Tracks
            .AsNoTracking()
            .Include(t => t.Distributions)
            .SingleOrDefaultAsync(t => t.Id == trackId, cancellationToken);

        if (track is null)
        {
            return null;
        }

        var artist = await db.Artists
            .AsNoTracking()
            .Where(a => a.Id == track.ArtistId)
            .Select(a => new TrackArtistResponse(a.Id, a.Name, a.Country))
            .SingleAsync(cancellationToken);

        var dspIds = track.Distributions.Select(d => d.DspId).ToList();
        var dspNames = await db.Dsps
            .AsNoTracking()
            .Where(d => dspIds.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id, d => d.Name, cancellationToken);

        var distributions = track.Distributions
            .OrderBy(d => dspNames.GetValueOrDefault(d.DspId))
            .Select(d => new TrackDistributionResponse(
                d.Id,
                d.DspId,
                dspNames.GetValueOrDefault(d.DspId, "Unknown DSP"),
                d.Status.Name,
                d.SubmittedAtUtc))
            .ToList();

        return new TrackDetailsResponse(
            track.Id,
            track.Title,
            track.Isrc.ToDisplayString(),
            artist,
            track.Genre.Name,
            track.Status.Name,
            track.ReleaseDate,
            track.CreatedAtUtc,
            track.UpdatedAtUtc,
            [.. track.Status.AllowedTransitions.Select(s => s.Name)],
            distributions);
    }
}

using ErrorOr;

using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using TrackManagement.Application.Common.Interfaces;
using TrackManagement.Application.Common.Models;
using TrackManagement.Application.Common.Querying;
using TrackManagement.Domain.Tracks;

namespace TrackManagement.Application.Tracks.Queries.ListTracks;

public sealed record ListTracksQuery(
    Guid? ArtistId,
    string? Genre,
    string? Status,
    int Page = 1,
    int PageSize = ListTracksQuery.DefaultPageSize) : IRequest<ErrorOr<PagedResponse<TrackSummaryResponse>>>
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
}

public sealed class ListTracksQueryValidator : AbstractValidator<ListTracksQuery>
{
    public ListTracksQueryValidator()
    {
        RuleFor(x => x.Genre)
            .Must(genre => Genre.TryFromName(genre, ignoreCase: true, out _))
            .When(x => !string.IsNullOrWhiteSpace(x.Genre))
            .WithMessage($"Unknown genre. Allowed: {string.Join(", ", Genre.List.Select(g => g.Name))}.");

        RuleFor(x => x.Status)
            .Must(status => TrackStatus.TryFromName(status, ignoreCase: true, out _))
            .When(x => !string.IsNullOrWhiteSpace(x.Status))
            .WithMessage($"Unknown status. Allowed: {string.Join(", ", TrackStatus.List.Select(s => s.Name))}.");

        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, ListTracksQuery.MaxPageSize);
    }
}

public sealed class ListTracksQueryHandler(IApplicationDbContext db)
    : IRequestHandler<ListTracksQuery, ErrorOr<PagedResponse<TrackSummaryResponse>>>
{
    public async Task<ErrorOr<PagedResponse<TrackSummaryResponse>>> Handle(
        ListTracksQuery request,
        CancellationToken cancellationToken)
    {
        var tracks = db.Tracks.AsNoTracking();

        if (request.ArtistId is { } artistId)
        {
            tracks = tracks.Where(t => t.ArtistId == artistId);
        }

        if (!string.IsNullOrWhiteSpace(request.Genre))
        {
            tracks = tracks.WhereEquals(t => t.Genre, Genre.FromName(request.Genre, ignoreCase: true));
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            tracks = tracks.WhereEquals(t => t.Status, TrackStatus.FromName(request.Status, ignoreCase: true));
        }

        var totalCount = await tracks.CountAsync(cancellationToken);

        var rows = await tracks
            .Join(
                db.Artists,
                track => track.ArtistId,
                artist => artist.Id,
                (track, artist) => new
                {
                    track.Id,
                    track.Title,
                    track.Isrc,
                    track.ArtistId,
                    ArtistName = artist.Name,
                    track.Genre,
                    track.Status,
                    track.ReleaseDate,
                    DistributionCount = track.Distributions.Count(),
                })
            .OrderByDescending(r => r.ReleaseDate)
            .ThenBy(r => r.Title)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(r => new TrackSummaryResponse(
                r.Id,
                r.Title,
                r.Isrc.ToDisplayString(),
                r.ArtistId,
                r.ArtistName,
                r.Genre.Name,
                r.Status.Name,
                r.ReleaseDate,
                r.DistributionCount))
            .ToList();

        return new PagedResponse<TrackSummaryResponse>(items, request.Page, request.PageSize, totalCount);
    }
}

using ErrorOr;

using MediatR;

using Microsoft.EntityFrameworkCore;

using TrackManagement.Application.Common.Interfaces;

namespace TrackManagement.Application.Artists.Queries.ListArtists;

public sealed record ListArtistsQuery : IRequest<ErrorOr<List<ArtistResponse>>>;

public sealed class ListArtistsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<ListArtistsQuery, ErrorOr<List<ArtistResponse>>>
{
    public async Task<ErrorOr<List<ArtistResponse>>> Handle(ListArtistsQuery request, CancellationToken cancellationToken) =>
        await db.Artists
            .AsNoTracking()
            .OrderBy(a => a.Name)
            .Select(a => new ArtistResponse(a.Id, a.Name, a.Email, a.Country, a.CreatedAtUtc))
            .ToListAsync(cancellationToken);
}

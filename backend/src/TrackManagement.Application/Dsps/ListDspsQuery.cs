using ErrorOr;

using MediatR;

using Microsoft.EntityFrameworkCore;

using TrackManagement.Application.Common.Interfaces;

namespace TrackManagement.Application.Dsps;

public sealed record DspResponse(Guid Id, string Name);

public sealed record ListDspsQuery : IRequest<ErrorOr<List<DspResponse>>>;

public sealed class ListDspsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<ListDspsQuery, ErrorOr<List<DspResponse>>>
{
    public async Task<ErrorOr<List<DspResponse>>> Handle(ListDspsQuery request, CancellationToken cancellationToken) =>
        await db.Dsps
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => new DspResponse(d.Id, d.Name))
            .ToListAsync(cancellationToken);
}

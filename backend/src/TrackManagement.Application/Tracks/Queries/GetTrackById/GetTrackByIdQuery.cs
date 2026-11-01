using ErrorOr;

using MediatR;

using TrackManagement.Application.Common.Interfaces;
using TrackManagement.Domain.Tracks;

namespace TrackManagement.Application.Tracks.Queries.GetTrackById;

public sealed record GetTrackByIdQuery(Guid TrackId) : IRequest<ErrorOr<TrackDetailsResponse>>;

public sealed class GetTrackByIdQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetTrackByIdQuery, ErrorOr<TrackDetailsResponse>>
{
    public async Task<ErrorOr<TrackDetailsResponse>> Handle(GetTrackByIdQuery request, CancellationToken cancellationToken)
    {
        var details = await TrackDetailsReader.ReadAsync(db, request.TrackId, cancellationToken);

        if (details is null)
        {
            return TrackErrors.NotFound(request.TrackId);
        }

        return details;
    }
}

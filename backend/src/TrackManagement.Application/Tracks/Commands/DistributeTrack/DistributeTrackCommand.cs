using ErrorOr;

using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using TrackManagement.Application.Common.Interfaces;
using TrackManagement.Domain.Dsps;
using TrackManagement.Domain.Tracks;

namespace TrackManagement.Application.Tracks.Commands.DistributeTrack;

public sealed record DistributeTrackCommand(Guid TrackId, List<Guid> DspIds) : IRequest<ErrorOr<TrackDetailsResponse>>;

public sealed class DistributeTrackCommandValidator : AbstractValidator<DistributeTrackCommand>
{
    public const int MaxDspsPerRequest = 20;

    public DistributeTrackCommandValidator()
    {
        RuleFor(x => x.TrackId).NotEmpty();

        RuleFor(x => x.DspIds)
            .NotEmpty()
            .WithMessage("Select at least one DSP to distribute to.")
            .Must(ids => ids.Count <= MaxDspsPerRequest)
            .WithMessage($"A single request can target at most {MaxDspsPerRequest} DSPs.")
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("DSP ids must be unique.");

        RuleForEach(x => x.DspIds).NotEmpty();
    }
}

public sealed class DistributeTrackCommandHandler(IApplicationDbContext db, TimeProvider timeProvider)
    : IRequestHandler<DistributeTrackCommand, ErrorOr<TrackDetailsResponse>>
{
    public async Task<ErrorOr<TrackDetailsResponse>> Handle(DistributeTrackCommand request, CancellationToken cancellationToken)
    {
        var track = await db.Tracks
            .Include(t => t.Distributions)
            .SingleOrDefaultAsync(t => t.Id == request.TrackId, cancellationToken);

        if (track is null)
        {
            return TrackErrors.NotFound(request.TrackId);
        }

        var knownDspIds = await db.Dsps
            .Where(d => request.DspIds.Contains(d.Id))
            .Select(d => d.Id)
            .ToListAsync(cancellationToken);

        var unknownDspIds = request.DspIds.Except(knownDspIds).ToList();
        if (unknownDspIds.Count > 0)
        {
            return DspErrors.NotFound(unknownDspIds);
        }

        var result = track.DistributeTo(request.DspIds, timeProvider.GetUtcNow());
        if (result.IsError)
        {
            return result.Errors;
        }

        await db.SaveChangesAsync(cancellationToken);

        return (await TrackDetailsReader.ReadAsync(db, track.Id, cancellationToken))!;
    }
}

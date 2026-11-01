using ErrorOr;

using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using TrackManagement.Application.Common.Interfaces;
using TrackManagement.Domain.Tracks;

namespace TrackManagement.Application.Tracks.Commands.UpdateTrackStatus;

public sealed record UpdateTrackStatusCommand(Guid TrackId, string Status) : IRequest<ErrorOr<TrackDetailsResponse>>;

public sealed class UpdateTrackStatusCommandValidator : AbstractValidator<UpdateTrackStatusCommand>
{
    public UpdateTrackStatusCommandValidator()
    {
        RuleFor(x => x.TrackId).NotEmpty();

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(status => TrackStatus.TryFromName(status, ignoreCase: true, out _))
            .WithMessage($"Unknown status. Allowed: {string.Join(", ", TrackStatus.List.Select(s => s.Name))}.");
    }
}

public sealed class UpdateTrackStatusCommandHandler(IApplicationDbContext db)
    : IRequestHandler<UpdateTrackStatusCommand, ErrorOr<TrackDetailsResponse>>
{
    public async Task<ErrorOr<TrackDetailsResponse>> Handle(UpdateTrackStatusCommand request, CancellationToken cancellationToken)
    {
        var track = await db.Tracks
            .Include(t => t.Distributions)
            .SingleOrDefaultAsync(t => t.Id == request.TrackId, cancellationToken);

        if (track is null)
        {
            return TrackErrors.NotFound(request.TrackId);
        }

        var result = track.ChangeStatus(TrackStatus.FromName(request.Status, ignoreCase: true));
        if (result.IsError)
        {
            return result.Errors;
        }

        await db.SaveChangesAsync(cancellationToken);

        return (await TrackDetailsReader.ReadAsync(db, track.Id, cancellationToken))!;
    }
}

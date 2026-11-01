using ErrorOr;

using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using TrackManagement.Application.Common.Interfaces;
using TrackManagement.Application.Common.Querying;
using TrackManagement.Domain.Artists;
using TrackManagement.Domain.Tracks;

namespace TrackManagement.Application.Tracks.Commands.CreateTrack;

public sealed record CreateTrackCommand(
    string Title,
    Guid ArtistId,
    string Isrc,
    DateOnly ReleaseDate,
    string Genre) : IRequest<ErrorOr<TrackDetailsResponse>>;

public sealed class CreateTrackCommandValidator : AbstractValidator<CreateTrackCommand>
{
    private static readonly DateOnly EarliestReleaseDate = new(1900, 1, 1);

    public CreateTrackCommandValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(Track.TitleMaxLength);

        RuleFor(x => x.ArtistId)
            .NotEmpty();

        RuleFor(x => x.Isrc)
            .NotEmpty()
            .Must(Domain.Tracks.Isrc.IsValid)
            .WithMessage("ISRC must follow the CC-XXX-YY-NNNNN format, e.g. 'EG-TKW-24-00001'.");

        RuleFor(x => x.Genre)
            .NotEmpty()
            .Must(genre => Domain.Tracks.Genre.TryFromName(genre, ignoreCase: true, out _))
            .WithMessage($"Unknown genre. Allowed: {string.Join(", ", Domain.Tracks.Genre.List.Select(g => g.Name))}.");

        // Releases can be scheduled, but not absurdly far ahead.
        RuleFor(x => x.ReleaseDate)
            .GreaterThanOrEqualTo(EarliestReleaseDate)
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime).AddYears(2))
            .WithMessage("Release date must be between 1900-01-01 and two years from today.");
    }
}

public sealed class CreateTrackCommandHandler(IApplicationDbContext db)
    : IRequestHandler<CreateTrackCommand, ErrorOr<TrackDetailsResponse>>
{
    public async Task<ErrorOr<TrackDetailsResponse>> Handle(CreateTrackCommand request, CancellationToken cancellationToken)
    {
        if (!await db.Artists.AnyAsync(a => a.Id == request.ArtistId, cancellationToken))
        {
            return ArtistErrors.NotFound(request.ArtistId);
        }

        var isrcResult = Isrc.Create(request.Isrc);
        if (isrcResult.IsError)
        {
            return isrcResult.Errors;
        }

        var isrc = isrcResult.Value;

        // Friendly 409 for the common case; the unique index is the real guarantee under concurrency.
        if (await db.Tracks.WhereEquals(t => t.Isrc, isrc).AnyAsync(cancellationToken))
        {
            return TrackErrors.IsrcAlreadyExists(isrc);
        }

        var track = Track.Create(
            request.Title,
            request.ArtistId,
            isrc,
            request.ReleaseDate,
            Genre.FromName(request.Genre, ignoreCase: true));

        db.Tracks.Add(track);
        await db.SaveChangesAsync(cancellationToken);

        return (await TrackDetailsReader.ReadAsync(db, track.Id, cancellationToken))!;
    }
}

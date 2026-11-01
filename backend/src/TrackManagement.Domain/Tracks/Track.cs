using ErrorOr;

using TrackManagement.Domain.Common;

namespace TrackManagement.Domain.Tracks;

/// <summary>
/// Aggregate root. All business rules about a track's lifecycle and its DSP deliveries live here,
/// so handlers stay thin and the rules are unit-testable without a database.
/// </summary>
public sealed class Track : AuditableEntity<Guid>
{
    public const int TitleMaxLength = 200;

    private readonly List<TrackDistribution> _distributions = [];

    private Track(Guid id, string title, Guid artistId, Isrc isrc, DateOnly releaseDate, Genre genre) : base(id)
    {
        Title = title;
        ArtistId = artistId;
        Isrc = isrc;
        ReleaseDate = releaseDate;
        Genre = genre;
        Status = TrackStatus.Draft;
    }

    private Track() { }

    public string Title { get; private set; } = null!;

    public Guid ArtistId { get; private set; }

    public Isrc Isrc { get; private set; } = null!;

    public DateOnly ReleaseDate { get; private set; }

    public Genre Genre { get; private set; } = null!;

    public TrackStatus Status { get; private set; } = TrackStatus.Draft;

    public IReadOnlyCollection<TrackDistribution> Distributions => _distributions.AsReadOnly();

    public static Track Create(string title, Guid artistId, Isrc isrc, DateOnly releaseDate, Genre genre)
    {
        var track = new Track(Guid.CreateVersion7(), title.Trim(), artistId, isrc, releaseDate, genre);
        track.Raise(new TrackCreatedDomainEvent(track.Id, artistId, isrc.Value));
        return track;
    }

    public ErrorOr<Updated> ChangeStatus(TrackStatus newStatus)
    {
        if (Status == newStatus)
        {
            return TrackErrors.StatusUnchanged(newStatus);
        }

        if (!Status.CanTransitionTo(newStatus))
        {
            return TrackErrors.InvalidStatusTransition(Status, newStatus);
        }

        if (newStatus == TrackStatus.Distributed && !_distributions.Any(d => d.Status.IsActive))
        {
            return TrackErrors.NoActiveDistributions;
        }

        SetStatus(newStatus);
        return Result.Updated;
    }

    /// <summary>
    /// Submits the track to the given DSPs. A draft track automatically becomes "submitted".
    /// Re-submitting to a DSP that rejected the track is allowed; re-submitting to a pending/live one is not.
    /// </summary>
    public ErrorOr<Success> DistributeTo(IReadOnlyCollection<Guid> dspIds, DateTimeOffset submittedAtUtc)
    {
        var requested = dspIds.Distinct().ToList();

        if (requested.Count == 0)
        {
            return TrackErrors.NoDspsSelected;
        }

        var alreadyActive = requested
            .Where(dspId => _distributions.Any(d => d.DspId == dspId && d.Status.IsActive))
            .ToList();

        if (alreadyActive.Count > 0)
        {
            return TrackErrors.AlreadyDistributedTo(alreadyActive);
        }

        foreach (var dspId in requested)
        {
            var rejected = _distributions.SingleOrDefault(d => d.DspId == dspId);
            if (rejected is not null)
            {
                rejected.Resubmit(submittedAtUtc);
            }
            else
            {
                _distributions.Add(new TrackDistribution(Id, dspId, submittedAtUtc));
            }
        }

        if (Status == TrackStatus.Draft)
        {
            SetStatus(TrackStatus.Submitted);
        }

        Raise(new TrackSubmittedToDspsDomainEvent(Id, requested));
        return Result.Success;
    }

    private void SetStatus(TrackStatus newStatus)
    {
        var previous = Status;
        Status = newStatus;
        Raise(new TrackStatusChangedDomainEvent(Id, previous.Name, newStatus.Name));
    }
}

public sealed record TrackCreatedDomainEvent(Guid TrackId, Guid ArtistId, string Isrc) : IDomainEvent;

public sealed record TrackStatusChangedDomainEvent(Guid TrackId, string FromStatus, string ToStatus) : IDomainEvent;

public sealed record TrackSubmittedToDspsDomainEvent(Guid TrackId, IReadOnlyCollection<Guid> DspIds) : IDomainEvent;

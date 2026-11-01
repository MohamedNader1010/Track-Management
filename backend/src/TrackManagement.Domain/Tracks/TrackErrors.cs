using ErrorOr;

namespace TrackManagement.Domain.Tracks;

public static class TrackErrors
{
    public static Error NotFound(Guid trackId) =>
        Error.NotFound("Track.NotFound", $"Track '{trackId}' was not found.");

    public static Error IsrcAlreadyExists(Isrc isrc) =>
        Error.Conflict("Track.IsrcAlreadyExists", $"A track with ISRC '{isrc.ToDisplayString()}' already exists.");

    public static Error StatusUnchanged(TrackStatus status) =>
        Error.Conflict("Track.StatusUnchanged", $"Track is already '{status.Name}'.");

    public static Error InvalidStatusTransition(TrackStatus from, TrackStatus to) =>
        Error.Conflict(
            "Track.InvalidStatusTransition",
            $"Cannot move a track from '{from.Name}' to '{to.Name}'. Allowed: {Describe(from.AllowedTransitions)}.");

    public static readonly Error NoActiveDistributions = Error.Conflict(
        "Track.NoActiveDistributions",
        "A track can only be marked 'distributed' after it has been submitted to at least one DSP.");

    public static readonly Error NoDspsSelected = Error.Validation(
        "DspIds",
        "Select at least one DSP to distribute to.");

    public static Error AlreadyDistributedTo(IEnumerable<Guid> dspIds) =>
        Error.Conflict(
            "Track.AlreadyDistributed",
            $"Track already has a pending or live distribution for DSP(s): {string.Join(", ", dspIds)}.");

    private static string Describe(IReadOnlyCollection<TrackStatus> statuses) =>
        statuses.Count == 0 ? "none (final state)" : string.Join(", ", statuses.Select(s => s.Name));
}

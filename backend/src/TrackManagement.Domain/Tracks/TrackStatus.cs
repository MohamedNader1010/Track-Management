using Ardalis.SmartEnum;

namespace TrackManagement.Domain.Tracks;

/// <summary>
/// Smart enum: the allowed lifecycle lives next to the values instead of being scattered across handlers.
/// draft → submitted → distributed, and a submitted track may be pulled back to draft.
/// </summary>
public sealed class TrackStatus : SmartEnum<TrackStatus>
{
    public static readonly TrackStatus Draft = new("draft", 1);
    public static readonly TrackStatus Submitted = new("submitted", 2);
    public static readonly TrackStatus Distributed = new("distributed", 3);

    private TrackStatus(string name, int value) : base(name, value) { }

    public IReadOnlyCollection<TrackStatus> AllowedTransitions => Value switch
    {
        1 => [Submitted],
        2 => [Draft, Distributed],
        _ => [],
    };

    public bool CanTransitionTo(TrackStatus next) => AllowedTransitions.Contains(next);
}

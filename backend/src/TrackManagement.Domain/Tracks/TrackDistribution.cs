using TrackManagement.Domain.Common;

namespace TrackManagement.Domain.Tracks;

/// <summary>A track's delivery to one DSP. Part of the <see cref="Track"/> aggregate — only changed through it.</summary>
public sealed class TrackDistribution : Entity<Guid>
{
    internal TrackDistribution(Guid trackId, Guid dspId, DateTimeOffset submittedAtUtc)
        : base(Guid.CreateVersion7())
    {
        TrackId = trackId;
        DspId = dspId;
        SubmittedAtUtc = submittedAtUtc;
        Status = DistributionStatus.Pending;
    }

    private TrackDistribution() { }

    public Guid TrackId { get; private set; }

    public Guid DspId { get; private set; }

    public DateTimeOffset SubmittedAtUtc { get; private set; }

    public DistributionStatus Status { get; private set; } = DistributionStatus.Pending;

    /// <summary>Called when the DSP acknowledges the delivery (webhook / polling in a real system).</summary>
    public void MarkLive() => Status = DistributionStatus.Live;

    public void MarkRejected() => Status = DistributionStatus.Rejected;

    internal void Resubmit(DateTimeOffset submittedAtUtc)
    {
        Status = DistributionStatus.Pending;
        SubmittedAtUtc = submittedAtUtc;
    }
}

using Ardalis.SmartEnum;

namespace TrackManagement.Domain.Tracks;

public sealed class DistributionStatus : SmartEnum<DistributionStatus>
{
    public static readonly DistributionStatus Pending = new("pending", 1);
    public static readonly DistributionStatus Live = new("live", 2);
    public static readonly DistributionStatus Rejected = new("rejected", 3);

    private DistributionStatus(string name, int value) : base(name, value) { }

    /// <summary>Pending and live distributions block re-submitting to the same DSP; rejected ones may be retried.</summary>
    public bool IsActive => this != Rejected;
}

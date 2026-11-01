using TrackManagement.Domain.Tracks;

namespace TrackManagement.Domain.UnitTests.Tracks;

public sealed class TrackStatusTests
{
    public static TheoryData<TrackStatus, TrackStatus, bool> Transitions => new()
    {
        { TrackStatus.Draft, TrackStatus.Submitted, true },
        { TrackStatus.Draft, TrackStatus.Distributed, false },
        { TrackStatus.Submitted, TrackStatus.Draft, true },
        { TrackStatus.Submitted, TrackStatus.Distributed, true },
        { TrackStatus.Distributed, TrackStatus.Draft, false },
        { TrackStatus.Distributed, TrackStatus.Submitted, false },
    };

    [Theory]
    [MemberData(nameof(Transitions))]
    public void CanTransitionTo_matches_the_lifecycle(TrackStatus from, TrackStatus to, bool expected)
    {
        from.CanTransitionTo(to).ShouldBe(expected);
    }

    [Theory]
    [InlineData("draft")]
    [InlineData("SUBMITTED")]
    [InlineData("Distributed")]
    public void Names_parse_case_insensitively(string name)
    {
        TrackStatus.TryFromName(name, ignoreCase: true, out _).ShouldBeTrue();
    }
}

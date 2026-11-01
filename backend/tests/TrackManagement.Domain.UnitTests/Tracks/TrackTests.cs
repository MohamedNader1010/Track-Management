using ErrorOr;

using TrackManagement.Domain.Tracks;

namespace TrackManagement.Domain.UnitTests.Tracks;

public sealed class TrackTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid Spotify = Guid.CreateVersion7();
    private static readonly Guid Anghami = Guid.CreateVersion7();

    private static Track NewTrack() => Track.Create(
        "  Leil El Qahira  ",
        Guid.CreateVersion7(),
        Isrc.Create("EG-TKW-26-00007").Value,
        new DateOnly(2026, 12, 4),
        Genre.ArabicPop);

    [Fact]
    public void Create_starts_as_draft_trims_title_and_raises_event()
    {
        var track = NewTrack();

        track.Status.ShouldBe(TrackStatus.Draft);
        track.Title.ShouldBe("Leil El Qahira");
        track.Distributions.ShouldBeEmpty();
        track.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<TrackCreatedDomainEvent>();
    }

    [Fact]
    public void DistributeTo_from_draft_creates_pending_distributions_and_submits_track()
    {
        var track = NewTrack();
        track.ClearDomainEvents();

        var result = track.DistributeTo([Spotify, Anghami], Now);

        result.IsError.ShouldBeFalse();
        track.Status.ShouldBe(TrackStatus.Submitted);
        track.Distributions.Count.ShouldBe(2);
        track.Distributions.ShouldAllBe(d => d.Status == DistributionStatus.Pending && d.SubmittedAtUtc == Now);
        track.DomainEvents.OfType<TrackStatusChangedDomainEvent>().ShouldHaveSingleItem();
        track.DomainEvents.OfType<TrackSubmittedToDspsDomainEvent>().ShouldHaveSingleItem().DspIds.Count.ShouldBe(2);
    }

    [Fact]
    public void DistributeTo_ignores_duplicate_ids_in_the_same_request()
    {
        var track = NewTrack();

        track.DistributeTo([Spotify, Spotify], Now).IsError.ShouldBeFalse();

        track.Distributions.ShouldHaveSingleItem();
    }

    [Fact]
    public void DistributeTo_same_dsp_twice_is_a_conflict()
    {
        var track = NewTrack();
        track.DistributeTo([Spotify], Now);

        var result = track.DistributeTo([Spotify], Now.AddMinutes(5));

        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorType.Conflict);
        result.FirstError.Code.ShouldBe("Track.AlreadyDistributed");
    }

    [Fact]
    public void DistributeTo_a_dsp_that_rejected_the_track_resubmits_the_same_row()
    {
        var track = NewTrack();
        track.DistributeTo([Spotify], Now);
        var distribution = track.Distributions.Single();
        distribution.MarkRejected();

        var result = track.DistributeTo([Spotify], Now.AddDays(1));

        result.IsError.ShouldBeFalse();
        track.Distributions.ShouldHaveSingleItem().ShouldBeSameAs(distribution);
        distribution.Status.ShouldBe(DistributionStatus.Pending);
        distribution.SubmittedAtUtc.ShouldBe(Now.AddDays(1));
    }

    [Fact]
    public void DistributeTo_with_no_dsps_is_a_validation_error()
    {
        var result = NewTrack().DistributeTo([], Now);

        result.FirstError.Type.ShouldBe(ErrorType.Validation);
    }

    [Fact]
    public void ChangeStatus_rejects_skipping_from_draft_to_distributed()
    {
        var track = NewTrack();

        var result = track.ChangeStatus(TrackStatus.Distributed);

        result.FirstError.Code.ShouldBe("Track.InvalidStatusTransition");
        track.Status.ShouldBe(TrackStatus.Draft);
    }

    [Fact]
    public void ChangeStatus_to_same_status_is_a_conflict()
    {
        NewTrack().ChangeStatus(TrackStatus.Draft).FirstError.Code.ShouldBe("Track.StatusUnchanged");
    }

    [Fact]
    public void ChangeStatus_to_distributed_requires_an_active_distribution()
    {
        var track = NewTrack();
        track.DistributeTo([Spotify], Now);
        track.Distributions.Single().MarkRejected();

        var result = track.ChangeStatus(TrackStatus.Distributed);

        result.FirstError.Code.ShouldBe("Track.NoActiveDistributions");
    }

    [Fact]
    public void ChangeStatus_follows_the_lifecycle()
    {
        var track = NewTrack();
        track.DistributeTo([Spotify], Now);
        track.Distributions.Single().MarkLive();

        track.ChangeStatus(TrackStatus.Distributed).IsError.ShouldBeFalse();

        track.Status.ShouldBe(TrackStatus.Distributed);
        track.ChangeStatus(TrackStatus.Draft).IsError.ShouldBeTrue(); // distributed is final
    }
}

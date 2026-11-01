using Microsoft.Extensions.Time.Testing;

using TrackManagement.Application.Artists.Commands.CreateArtist;
using TrackManagement.Application.Tracks.Commands.CreateTrack;
using TrackManagement.Application.Tracks.Commands.DistributeTrack;
using TrackManagement.Application.Tracks.Queries.ListTracks;

namespace TrackManagement.Application.UnitTests.Validators;

public sealed class ValidatorTests
{
    private static readonly FakeTimeProvider Clock = new(new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero));

    [Theory]
    [InlineData("", "a@b.com", "EG", "Name")]
    [InlineData("Nour", "not-an-email", "EG", "Email")]
    [InlineData("Nour", "a@b.com", "EGY", "Country")]
    [InlineData("Nour", "a@b.com", "1A", "Country")]
    public void CreateArtist_rejects_bad_input(string name, string email, string country, string invalidProperty)
    {
        var result = new CreateArtistCommandValidator().Validate(new CreateArtistCommand(name, email, country));

        result.Errors.ShouldContain(e => e.PropertyName == invalidProperty);
    }

    [Fact]
    public void CreateTrack_accepts_a_valid_command()
    {
        var command = new CreateTrackCommand("Ana Mesh Ana", Guid.CreateVersion7(), "EG-TKW-24-00001", new DateOnly(2026, 1, 1), "mahraganat");

        new CreateTrackCommandValidator(Clock).Validate(command).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("EG-TKW-24-001", "Pop", "Isrc")]
    [InlineData("EG-TKW-24-00001", "Polka", "Genre")]
    public void CreateTrack_rejects_bad_isrc_and_genre(string isrc, string genre, string invalidProperty)
    {
        var command = new CreateTrackCommand("Song", Guid.CreateVersion7(), isrc, new DateOnly(2026, 1, 1), genre);

        new CreateTrackCommandValidator(Clock).Validate(command).Errors.ShouldContain(e => e.PropertyName == invalidProperty);
    }

    [Fact]
    public void CreateTrack_rejects_release_dates_too_far_in_the_future()
    {
        var command = new CreateTrackCommand("Song", Guid.CreateVersion7(), "EG-TKW-24-00001", new DateOnly(2030, 1, 1), "Pop");

        new CreateTrackCommandValidator(Clock).Validate(command).Errors.ShouldContain(e => e.PropertyName == "ReleaseDate");
    }

    [Fact]
    public void DistributeTrack_rejects_empty_and_duplicate_dsp_lists()
    {
        var validator = new DistributeTrackCommandValidator();
        var dsp = Guid.CreateVersion7();

        validator.Validate(new DistributeTrackCommand(Guid.CreateVersion7(), [])).IsValid.ShouldBeFalse();
        validator.Validate(new DistributeTrackCommand(Guid.CreateVersion7(), [dsp, dsp])).IsValid.ShouldBeFalse();
        validator.Validate(new DistributeTrackCommand(Guid.CreateVersion7(), [dsp])).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("published", null, 1, 20)]
    [InlineData(null, "Polka", 1, 20)]
    [InlineData(null, null, 0, 20)]
    [InlineData(null, null, 1, 500)]
    public void ListTracks_rejects_unknown_filters_and_bad_paging(string? status, string? genre, int page, int pageSize)
    {
        new ListTracksQueryValidator()
            .Validate(new ListTracksQuery(null, genre, status, page, pageSize))
            .IsValid.ShouldBeFalse();
    }
}

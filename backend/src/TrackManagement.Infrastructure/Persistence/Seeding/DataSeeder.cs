using Microsoft.EntityFrameworkCore;

using TrackManagement.Domain.Artists;
using TrackManagement.Domain.Dsps;
using TrackManagement.Domain.Tracks;

namespace TrackManagement.Infrastructure.Persistence.Seeding;

/// <summary>
/// Realistic MENA-flavoured sample catalogue. Runs through EF Core's UseSeeding/UseAsyncSeeding hooks,
/// i.e. on <c>dotnet ef database update</c> and on <c>Database.MigrateAsync()</c>. Idempotent.
/// Built through the domain model, so seeded data obeys the same rules as API-created data.
/// </summary>
internal static class DataSeeder
{
    public static async Task SeedAsync(DbContext context, TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        if (await context.Set<Artist>().AnyAsync(cancellationToken))
        {
            return;
        }

        var now = timeProvider.GetUtcNow();

        var spotify = Dsp.Create("Spotify");
        var appleMusic = Dsp.Create("Apple Music");
        var youTubeMusic = Dsp.Create("YouTube Music");
        var anghami = Dsp.Create("Anghami");
        context.AddRange(spotify, appleMusic, youTubeMusic, anghami);

        var nour = Artist.Create("Nour El-Sayed", "nour.elsayed@example.com", "EG");
        var layla = Artist.Create("Layla Haddad", "layla.haddad@example.com", "LB");
        var omar = Artist.Create("Omar Al-Rashid", "omar.alrashid@example.com", "SA");
        var yasmine = Artist.Create("Yasmine Benali", "yasmine.benali@example.com", "MA");
        context.AddRange(nour, layla, omar, yasmine);

        // Draft — not yet sent anywhere.
        var t1 = NewTrack("Leil El Qahira", nour, "EG-TKW-26-00007", new DateOnly(2026, 12, 4), Genre.ArabicPop);
        var t2 = NewTrack("Sahra Fel Shar3", nour, "EG-TKW-26-00008", new DateOnly(2027, 1, 15), Genre.Mahraganat);
        var t3 = NewTrack("Beirut Blue", layla, "LB-TKW-26-00003", new DateOnly(2026, 11, 20), Genre.Jazz);

        // Submitted — waiting on the DSPs (one already rejected by a DSP).
        var t4 = NewTrack("Ya Hawa", layla, "LB-TKW-25-00012", new DateOnly(2025, 10, 3), Genre.ArabicPop);
        Submit(t4, now.AddDays(-3), spotify, anghami);

        var t5 = NewTrack("Desert Lights", omar, "SA-TKW-25-00021", new DateOnly(2025, 9, 12), Genre.Electronic);
        Submit(t5, now.AddDays(-6), spotify, appleMusic, youTubeMusic);
        t5.Distributions.Single(d => d.DspId == appleMusic.Id).MarkRejected();

        // Distributed — live on at least one DSP.
        var t6 = NewTrack("Ana Mesh Ana", nour, "EG-TKW-24-00001", new DateOnly(2024, 6, 21), Genre.Mahraganat);
        Distribute(t6, now.AddDays(-120), spotify, appleMusic, youTubeMusic, anghami);

        var t7 = NewTrack("Khaleej Nights", omar, "SA-TKW-24-00002", new DateOnly(2024, 3, 8), Genre.Khaleeji);
        Distribute(t7, now.AddDays(-200), anghami, spotify);

        var t8 = NewTrack("Casablanca Rain", yasmine, "MA-TKW-25-00004", new DateOnly(2025, 2, 14), Genre.Rai);
        Distribute(t8, now.AddDays(-90), spotify, youTubeMusic);

        var t9 = NewTrack("Atlas", yasmine, "MA-TKW-25-00005", new DateOnly(2025, 5, 30), Genre.HipHop);
        Distribute(t9, now.AddDays(-60), spotify, appleMusic, youTubeMusic, anghami);

        var t10 = NewTrack("Tarab Session No. 1", layla, "LB-TKW-23-00009", new DateOnly(2023, 11, 11), Genre.Tarab);
        Distribute(t10, now.AddDays(-400), appleMusic, anghami);

        context.AddRange(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);

        await context.SaveChangesAsync(cancellationToken);
    }

    private static Track NewTrack(string title, Artist artist, string isrc, DateOnly releaseDate, Genre genre) =>
        Track.Create(title, artist.Id, Isrc.Create(isrc).Value, releaseDate, genre);

    private static void Submit(Track track, DateTimeOffset at, params Dsp[] dsps) =>
        Ensure(track.DistributeTo([.. dsps.Select(d => d.Id)], at).IsError, track);

    private static void Distribute(Track track, DateTimeOffset at, params Dsp[] dsps)
    {
        Submit(track, at, dsps);

        foreach (var distribution in track.Distributions)
        {
            distribution.MarkLive();
        }

        Ensure(track.ChangeStatus(TrackStatus.Distributed).IsError, track);
    }

    private static void Ensure(bool isError, Track track)
    {
        if (isError)
        {
            throw new InvalidOperationException($"Seed data for track '{track.Title}' violates a domain rule.");
        }
    }
}

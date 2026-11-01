using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Mvc;

using TrackManagement.Api.IntegrationTests.Infrastructure;
using TrackManagement.Application.Artists;
using TrackManagement.Application.Common.Models;
using TrackManagement.Application.Dsps;
using TrackManagement.Application.Tracks;

namespace TrackManagement.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class TracksEndpointsTests(ApiFactory factory)
{
    [Fact]
    public async Task List_returns_the_seeded_catalogue_anonymously()
    {
        var client = factory.CreateClient();

        var page = await client.GetFromJsonAsync<PagedResponse<TrackSummaryResponse>>("/api/tracks?pageSize=100");

        page!.TotalCount.ShouldBeGreaterThanOrEqualTo(8);
        page.Items.Select(t => t.Status).Distinct().Count().ShouldBe(3);
        page.Items.ShouldAllBe(t => !string.IsNullOrEmpty(t.ArtistName));
    }

    [Fact]
    public async Task List_filters_by_status()
    {
        var client = factory.CreateClient();

        var page = await client.GetFromJsonAsync<PagedResponse<TrackSummaryResponse>>("/api/tracks?status=DISTRIBUTED");

        page!.Items.ShouldNotBeEmpty();
        page.Items.ShouldAllBe(t => t.Status == "distributed");
    }

    [Fact]
    public async Task List_with_unknown_status_returns_validation_problem()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/tracks?status=published");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem!.Errors.ShouldContainKey("Status");
    }

    [Fact]
    public async Task GetById_unknown_track_returns_404()
    {
        var response = await factory.CreateClient().GetAsync($"/api/tracks/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_without_token_returns_401()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/tracks", new { title = "x" });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_with_token_missing_the_role_returns_403()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ApiFactory.ViewerUsername, ApiFactory.ViewerPassword);

        var response = await client.PostAsJsonAsync("/api/tracks", new { title = "x" });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Full_lifecycle_create_distribute_and_publish()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var artist = (await client.GetFromJsonAsync<List<ArtistResponse>>("/api/artists"))!.First();
        var dsps = (await client.GetFromJsonAsync<List<DspResponse>>("/api/dsps"))!;

        // Create → draft
        var createResponse = await client.PostAsJsonAsync("/api/tracks", new
        {
            title = "Integration Anthem",
            artistId = artist.Id,
            isrc = NewIsrc(),
            releaseDate = "2026-06-01",
            genre = "Arabic Pop",
        });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        createResponse.Headers.Location.ShouldNotBeNull();
        var track = (await createResponse.Content.ReadFromJsonAsync<TrackDetailsResponse>())!;
        track.Status.ShouldBe("draft");

        // Skipping straight to distributed is not allowed
        var skip = await client.PatchAsJsonAsync($"/api/tracks/{track.Id}/status", new { status = "distributed" });
        skip.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        // Distribute → submitted with pending distributions
        var distribute = await client.PostAsJsonAsync($"/api/tracks/{track.Id}/distribute", new { dspIds = new[] { dsps[0].Id, dsps[1].Id } });
        distribute.StatusCode.ShouldBe(HttpStatusCode.OK);
        var distributed = (await distribute.Content.ReadFromJsonAsync<TrackDetailsResponse>())!;
        distributed.Status.ShouldBe("submitted");
        distributed.Distributions.Count.ShouldBe(2);
        distributed.Distributions.ShouldAllBe(d => d.Status == "pending");

        // Same DSP again → 409
        var again = await client.PostAsJsonAsync($"/api/tracks/{track.Id}/distribute", new { dspIds = new[] { dsps[0].Id } });
        again.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        // Submitted → distributed
        var publish = await client.PatchAsJsonAsync($"/api/tracks/{track.Id}/status", new { status = "distributed" });
        publish.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await publish.Content.ReadFromJsonAsync<TrackDetailsResponse>())!.Status.ShouldBe("distributed");
    }

    [Fact]
    public async Task Create_with_duplicate_isrc_returns_409()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var artist = (await client.GetFromJsonAsync<List<ArtistResponse>>("/api/artists"))!.First();
        var body = new { title = "Dup", artistId = artist.Id, isrc = NewIsrc(), releaseDate = "2026-01-01", genre = "Pop" };

        (await client.PostAsJsonAsync("/api/tracks", body)).StatusCode.ShouldBe(HttpStatusCode.Created);
        (await client.PostAsJsonAsync("/api/tracks", body)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Create_with_invalid_body_returns_all_field_errors()
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/tracks", new { title = "", isrc = "nope", genre = "Polka", releaseDate = "2026-01-01" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>())!;
        problem.Errors.Keys.ShouldBeSubsetOf(["Title", "ArtistId", "Isrc", "Genre"]);
        problem.Errors.Count.ShouldBe(4);
    }

    private static string NewIsrc() => $"EGTST{Random.Shared.Next(0, 10_000_000):D7}";
}

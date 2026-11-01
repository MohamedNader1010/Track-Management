using System.Net;
using System.Net.Http.Json;

using TrackManagement.Api.IntegrationTests.Infrastructure;
using TrackManagement.Application.Artists;

namespace TrackManagement.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class AuthAndArtistsEndpointsTests(ApiFactory factory)
{
    [Fact]
    public async Task Token_with_wrong_password_returns_401_problem()
    {
        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/auth/token",
            new { username = ApiFactory.ManagerUsername, password = "wrong" });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task Me_returns_the_caller_identity()
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        var me = await client.GetFromJsonAsync<MeDto>("/api/auth/me");

        me!.Username.ShouldBe(ApiFactory.ManagerUsername);
        me.Roles.ShouldContain("catalog-manager");
    }

    [Fact]
    public async Task Artists_list_contains_seed_data()
    {
        var artists = await factory.CreateClient().GetFromJsonAsync<List<ArtistResponse>>("/api/artists");

        artists!.Count.ShouldBeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task Create_artist_then_duplicate_email_conflicts()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var email = $"artist-{Guid.NewGuid():N}@example.com";

        var created = await client.PostAsJsonAsync("/api/artists", new { name = "New Artist", email, country = "ae" });
        created.StatusCode.ShouldBe(HttpStatusCode.Created);
        (await created.Content.ReadFromJsonAsync<ArtistResponse>())!.Country.ShouldBe("AE");

        var duplicate = await client.PostAsJsonAsync("/api/artists", new { name = "Copy", email = email.ToUpperInvariant(), country = "EG" });
        duplicate.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    private sealed record MeDto(string Username, List<string> Roles);
}

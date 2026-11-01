using System.Net.Http.Headers;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

using Testcontainers.MsSql;

using TrackManagement.Application.Auth;

namespace TrackManagement.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Boots the real API against a throw-away SQL Server in Docker (Testcontainers): real migrations, real seeding,
/// real JWT validation. Requires Docker to be running.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string ManagerUsername = "manager";
    public const string ManagerPassword = "Integration@Test1";
    public const string ViewerUsername = "viewer";
    public const string ViewerPassword = "Integration@Test2";

    private readonly MsSqlContainer _sqlServer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting("ConnectionStrings:Default", _sqlServer.GetConnectionString());
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "true");
        builder.UseSetting("Jwt:SigningKey", "integration-tests-signing-key-0123456789abcdef");
        builder.UseSetting("RateLimiting:AuthPermitsPerMinute", "1000");

        builder.UseSetting("DemoUsers:Users:0:Username", ManagerUsername);
        builder.UseSetting("DemoUsers:Users:0:Password", ManagerPassword);
        builder.UseSetting("DemoUsers:Users:0:Roles:0", "catalog-manager");
        builder.UseSetting("DemoUsers:Users:1:Username", ViewerUsername);
        builder.UseSetting("DemoUsers:Users:1:Password", ViewerPassword);
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync(string username = ManagerUsername, string password = ManagerPassword)
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/token", new { username, password });
        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<TokenResponse>();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);
        return client;
    }

    public Task InitializeAsync() => _sqlServer.StartAsync();

    Task IAsyncLifetime.DisposeAsync() => _sqlServer.DisposeAsync().AsTask();
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}

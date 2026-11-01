using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using TrackManagement.Application.Common.Interfaces;
using TrackManagement.Application.Common.Security;
using TrackManagement.Infrastructure.Authentication;
using TrackManagement.Infrastructure.Persistence;
using TrackManagement.Infrastructure.Persistence.Interceptors;
using TrackManagement.Infrastructure.Persistence.Seeding;

namespace TrackManagement.Infrastructure;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddInfrastructure()
        {
            services.AddSingleton(TimeProvider.System);

            AddPersistence(services);
            AddJwtAuthentication(services);

            return services;
        }
    }

    private static void AddPersistence(IServiceCollection services)
    {
        services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();
        services.AddSingleton<SlowQueryInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            // Resolved lazily (not at registration) so test hosts and env vars can override it.
            var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString("Default")
                ?? throw new InvalidOperationException("Connection string 'ConnectionStrings:Default' is not configured.");

            var timeProvider = sp.GetRequiredService<TimeProvider>();

            options
                .UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(maxRetryCount: 3))
                .AddInterceptors(sp.GetServices<ISaveChangesInterceptor>())
                .AddInterceptors(sp.GetRequiredService<SlowQueryInterceptor>())
                // EF Core 9+ seeding hooks: run by `dotnet ef database update` and by Migrate().
                .UseAsyncSeeding((context, _, ct) => DataSeeder.SeedAsync(context, timeProvider, ct))
                .UseSeeding((context, _) => DataSeeder.SeedAsync(context, timeProvider, CancellationToken.None).GetAwaiter().GetResult());
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AppDbContext>());
    }

    private static void AddJwtAuthentication(IServiceCollection services)
    {
        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<DemoUsersOptions>()
            .BindConfiguration(DemoUsersOptions.SectionName);

        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IUserAuthenticator, DemoUserAuthenticator>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        // Configure bearer validation from the *validated* JwtOptions instead of reading raw config.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;

                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = AuthClaimTypes.Name,
                    RoleClaimType = AuthClaimTypes.Role,
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(Policies.ManageCatalog, policy => policy
                .RequireAuthenticatedUser()
                .RequireRole(Roles.CatalogManager));
    }
}

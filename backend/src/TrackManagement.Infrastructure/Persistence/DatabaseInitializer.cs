using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TrackManagement.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    /// <summary>Applies pending migrations (which also triggers the seeding hooks). Meant for dev / demo / tests.</summary>
    public static async Task ApplyMigrationsAsync(this IHost host, CancellationToken cancellationToken = default)
    {
        await using var scope = host.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DatabaseInitializer));

        var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        logger.LogInformation("Applying {Count} pending migration(s): {Migrations}", pending.Count, pending);

        await db.Database.MigrateAsync(cancellationToken);
    }
}

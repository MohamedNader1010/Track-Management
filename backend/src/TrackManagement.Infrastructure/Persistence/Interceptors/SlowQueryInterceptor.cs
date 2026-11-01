using System.Data.Common;

using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace TrackManagement.Infrastructure.Persistence.Interceptors;

/// <summary>Flags SQL commands slower than the threshold — cheap production insight without full SQL logging.</summary>
public sealed class SlowQueryInterceptor(ILogger<SlowQueryInterceptor> logger) : DbCommandInterceptor
{
    private static readonly TimeSpan Threshold = TimeSpan.FromMilliseconds(300);

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        LogIfSlow(command, eventData.Duration);
        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        LogIfSlow(command, eventData.Duration);
        return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }

    private void LogIfSlow(DbCommand command, TimeSpan duration)
    {
        if (duration > Threshold)
        {
            // SQL text only — parameter values are never logged (they may contain personal data).
            logger.LogWarning("Slow query ({ElapsedMs:0} ms): {CommandText}", duration.TotalMilliseconds, command.CommandText);
        }
    }
}

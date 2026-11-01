using ErrorOr;

using MediatR;

using Microsoft.Extensions.Logging;

namespace TrackManagement.Application.Common.Behaviors;

/// <summary>
/// Logs every request's name, outcome and duration. The request body is deliberately NOT logged:
/// commands such as <c>LoginCommand</c> carry passwords.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger,
    TimeProvider timeProvider)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly TimeSpan SlowRequestThreshold = TimeSpan.FromMilliseconds(500);

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var startedAt = timeProvider.GetTimestamp();

        logger.LogInformation("Handling {RequestName}", requestName);

        var response = await next();

        var elapsed = timeProvider.GetElapsedTime(startedAt);

        if (response is IErrorOr { IsError: true, Errors: { } errors })
        {
            logger.LogWarning(
                "{RequestName} failed in {ElapsedMs:0} ms with {ErrorCount} error(s): {ErrorCodes}",
                requestName,
                elapsed.TotalMilliseconds,
                errors.Count,
                string.Join(", ", errors.Select(e => e.Code)));
        }
        else if (elapsed > SlowRequestThreshold)
        {
            logger.LogWarning("{RequestName} was slow: {ElapsedMs:0} ms", requestName, elapsed.TotalMilliseconds);
        }
        else
        {
            logger.LogInformation("Handled {RequestName} in {ElapsedMs:0} ms", requestName, elapsed.TotalMilliseconds);
        }

        return response;
    }
}

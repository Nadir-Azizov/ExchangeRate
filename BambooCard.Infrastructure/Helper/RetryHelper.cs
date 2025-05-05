using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

namespace BambooCard.Infrastructure.Helper;

public static class RetryHelper
{
    public static IAsyncPolicy<HttpResponseMessage> CreateResiliencePolicy(
        int retryCount,
        int baseDelaySeconds,
        int allowedFailuresBeforeBreak,
        TimeSpan breakDuration,
        ILogger logger)
    {
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount,
                attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt - 1) * baseDelaySeconds),
                onRetry: (outcome, ts, attempt, ctx) =>
                    logger.LogWarning(
                        "Retry {Attempt}/{MaxRetries} failed – waiting {Delay}s",
                        attempt, retryCount, ts.TotalSeconds)
            );

        var breakerPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: allowedFailuresBeforeBreak,
                durationOfBreak: breakDuration,
                onBreak: (outcome, ts) =>
                    logger.LogError(
                        "Circuit opened for {BreakDuration}s due to: {Error}",
                        ts.TotalSeconds, outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString()),
                onReset: () =>
                    logger.LogInformation("Circuit closed; calls will flow again."),
                onHalfOpen: () =>
                    logger.LogInformation("Circuit half-open; testing service health.")
            );

        return Policy.WrapAsync(breakerPolicy, retryPolicy);
    }
}
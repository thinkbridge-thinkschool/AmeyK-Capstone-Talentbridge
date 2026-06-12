using Polly;
using Polly.CircuitBreaker;
using Polly.RateLimiting;
using Polly.Retry;
using Polly.Timeout;
using System.Threading.RateLimiting;

namespace TalentBridge.API.Resilience;

public static class TalentBridgeResiliencePolicies
{
    public static void ConfigureHttpPipeline(ResiliencePipelineBuilder<HttpResponseMessage> pipelineBuilder, ILogger logger)
    {
        pipelineBuilder
            .AddRateLimiter(new RateLimiterStrategyOptions
            {
                RateLimiter = args =>
                {
                    var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
                    {
                        PermitLimit = 10,
                        QueueLimit = 20
                    });
                    return limiter.AcquireAsync(1, args.Context.CancellationToken);
                },
                OnRejected = args =>
                {
                    logger.LogWarning("[Polly BULKHEAD] request rejected — too many concurrent");
                    return ValueTask.CompletedTask;
                }
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                FailureRatio = 1.0,
                MinimumThroughput = 5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromSeconds(30),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<Exception>()
                    .HandleResult(r => (int)r.StatusCode >= 500),
                OnOpened = args =>
                {
                    logger.LogWarning("[Polly CIRCUIT OPEN] breaking for {Duration}s — {Outcome}",
                        args.BreakDuration.TotalSeconds,
                        args.Outcome.Exception?.Message ?? args.Outcome.Result?.StatusCode.ToString());
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    logger.LogInformation("[Polly CIRCUIT CLOSED] service recovered");
                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = args =>
                {
                    logger.LogInformation("[Polly CIRCUIT HALF-OPEN] testing recovery...");
                    return ValueTask.CompletedTask;
                }
            })
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromSeconds(1),
                UseJitter = false,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<Exception>()
                    .HandleResult(r => (int)r.StatusCode >= 500),
                OnRetry = args =>
                {
                    var error = args.Outcome.Exception?.Message ?? args.Outcome.Result?.StatusCode.ToString();
                    logger.LogWarning("[Polly RETRY] attempt {Attempt} after {Delay}s: {Error}",
                        args.AttemptNumber + 1, args.RetryDelay.TotalSeconds, error);
                    return ValueTask.CompletedTask;
                }
            })
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(5),
                OnTimeout = args =>
                {
                    logger.LogWarning("[Polly TIMEOUT] request cancelled after {Seconds}s", args.Timeout.TotalSeconds);
                    return ValueTask.CompletedTask;
                }
            });
    }

    public static ResiliencePipeline<HttpResponseMessage> GetHttpResiliencePolicy(ILogger logger)
    {
        var builder = new ResiliencePipelineBuilder<HttpResponseMessage>();
        ConfigureHttpPipeline(builder, logger);
        return builder.Build();
    }
}

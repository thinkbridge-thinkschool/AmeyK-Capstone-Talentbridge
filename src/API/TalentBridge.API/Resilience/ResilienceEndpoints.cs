namespace TalentBridge.API.Resilience;

public static class ResilienceEndpoints
{
    private static bool _forceFailure = false;
    private static int _failureCount = 0;
    private static int _totalRequests = 0;

    public static void MapResilienceEndpoints(this WebApplication app)
    {
        app.MapPost("/api/resilience/force-failure/{enabled:bool}", (bool enabled) =>
        {
            _forceFailure = enabled;
            _failureCount = 0;
            return Results.Ok(new { ForceFailure = _forceFailure, Message = $"Failure simulation {(enabled ? "enabled" : "disabled")}" });
        })
        .WithTags("Resilience");

        app.MapGet("/api/resilience/status", () =>
        {
            return Results.Ok(new
            {
                ForceFailureEnabled = _forceFailure,
                TotalRequests = _totalRequests,
                FailureCount = _failureCount
            });
        })
        .WithTags("Resilience");

        app.MapGet("/api/resilience/test-call", async (IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("ResilienceTest");
            _totalRequests++;
            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                if (_forceFailure)
                {
                    _failureCount++;
                    logger.LogWarning("[ResilienceTest] Simulating failure #{Count}", _failureCount);
                    return Results.StatusCode(503);
                }

                var client = httpClientFactory.CreateClient("TalentBridgeClient");
                var response = await client.GetAsync("https://httpbin.org/get");

                sw.Stop();
                return Results.Ok(new
                {
                    Success = true,
                    StatusCode = (int)response.StatusCode,
                    DurationMs = sw.ElapsedMilliseconds,
                    Message = "Request succeeded"
                });
            }
            catch (Exception ex)
            {
                sw.Stop();
                logger.LogError(ex, "[ResilienceTest] Request failed after {Ms}ms", sw.ElapsedMilliseconds);
                return Results.Problem($"Request failed: {ex.GetType().Name} — {ex.Message}");
            }
        })
        .WithTags("Resilience");
    }
}

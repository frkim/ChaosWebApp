using ChaosWebApp.Services;

namespace ChaosWebApp.Middleware;

public class ChaosMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ChaosMiddleware> _logger;

    public ChaosMiddleware(RequestDelegate next, ILogger<ChaosMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IChaosService chaosService)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Skip chaos for swagger UI, health checks, and static files
        if (path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/health", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/css", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/js", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/lib", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/favicon", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // Determine context
        var requestContext = path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase) ? "api" : "app";

        if (!chaosService.ShouldTriggerChaos(requestContext))
        {
            await _next(context);
            return;
        }

        var activeTypes = chaosService.GetActiveChaosTypes();
        if (activeTypes.Count == 0)
        {
            await _next(context);
            return;
        }

        var chaosType = activeTypes[Random.Shared.Next(activeTypes.Count)];
        var cfg = chaosService.GetConfig();

        _logger.LogWarning("🔥 Chaos triggered: {ChaosType} on {Path}", chaosType, path);

        switch (chaosType)
        {
            case "HighCpu":
                await BurnCpuAsync(cfg.CpuDurationMs);
                await _next(context);
                break;

            case "HighMemory":
                await AllocateMemoryAsync(cfg.MemorySizeMb);
                await _next(context);
                break;

            case "HighLatency":
                await Task.Delay(cfg.LatencyMs);
                await _next(context);
                break;

            case "SlowResponse":
                await _next(context);
                await Task.Delay(cfg.SlowResponseMs);
                break;

            case "Error404":
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Not Found",
                    message = "Chaos Engineering: 404 injected",
                    path = path,
                    timestamp = DateTime.UtcNow
                });
                break;

            case "Error500":
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Internal Server Error",
                    message = "Chaos Engineering: 500 injected",
                    path = path,
                    timestamp = DateTime.UtcNow
                });
                break;

            case "Error503":
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                context.Response.Headers["Retry-After"] = "30";
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Service Unavailable",
                    message = "Chaos Engineering: 503 injected",
                    path = path,
                    timestamp = DateTime.UtcNow
                });
                break;

            case "Error429":
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.Headers["Retry-After"] = "60";
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Too Many Requests",
                    message = "Chaos Engineering: 429 injected",
                    path = path,
                    timestamp = DateTime.UtcNow
                });
                break;

            case "StackOverflow":
                // Simulate stack overflow by returning 500 with descriptive error
                // Note: Real StackOverflowException terminates the CLR and cannot be caught
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Stack Overflow",
                    message = "Chaos Engineering: StackOverflowException simulated — infinite recursion detected",
                    exceptionType = "System.StackOverflowException",
                    path = path,
                    timestamp = DateTime.UtcNow
                });
                break;

            case "RandomErrors":
                var errorCodes = new[] { 400, 401, 403, 404, 408, 409, 422, 429, 500, 502, 503 };
                var randomCode = errorCodes[Random.Shared.Next(errorCodes.Length)];
                context.Response.StatusCode = randomCode;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = $"HTTP {randomCode}",
                    message = $"Chaos Engineering: Random HTTP {randomCode} injected",
                    path = path,
                    timestamp = DateTime.UtcNow
                });
                break;

            default:
                await _next(context);
                break;
        }
    }

    private static async Task BurnCpuAsync(int durationMs)
    {
        await Task.Run(() =>
        {
            var end = DateTime.UtcNow.AddMilliseconds(durationMs);
            while (DateTime.UtcNow < end)
            {
                // Tight CPU loop
                for (var i = 0; i < 1_000_000; i++) { _ = Math.Sqrt(i); }
            }
        });
    }

    private static async Task AllocateMemoryAsync(int sizeMb)
    {
        // Allocate but hold briefly then release
        var chunks = new List<byte[]>();
        try
        {
            for (var i = 0; i < sizeMb; i++)
                chunks.Add(new byte[1024 * 1024]);

            await Task.Delay(200); // Hold briefly without blocking the thread pool
        }
        finally
        {
            chunks.Clear();
            GC.Collect();
        }
    }
}

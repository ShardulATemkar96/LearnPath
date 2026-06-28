using System.Collections.Concurrent;

namespace LearnPath.API.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;

    private static readonly ConcurrentDictionary<string, (int Count, DateTime WindowStart)>
        _requests = new();

    private const int MAX_REQUESTS = 100;
    private static readonly TimeSpan WINDOW = TimeSpan.FromMinutes(1);

    public RateLimitingMiddleware(
        RequestDelegate next,
        ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api/v1/health"))
        {
            await _next(context);
            return;
        }

        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var now = DateTime.UtcNow;

        var entry = _requests.AddOrUpdate(
            ip,
            _ => (1, now),
            (_, existing) =>
            {
                if (now - existing.WindowStart > WINDOW)
                    return (1, now);
                return (existing.Count + 1, existing.WindowStart);
            });

        context.Response.Headers["X-RateLimit-Limit"] = MAX_REQUESTS.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] =
            Math.Max(0, MAX_REQUESTS - entry.Count).ToString();

        if (entry.Count > MAX_REQUESTS)
        {
            _logger.LogWarning("Rate limit exceeded for IP: {IP}", ip);
            context.Response.StatusCode = 429;

            await context.Response.WriteAsJsonAsync(new
            {
                Success = false,
                Message = "Too many requests. Please slow down.",
                Timestamp = DateTime.UtcNow,
            });

            return;
        }

        await _next(context);
    }
}

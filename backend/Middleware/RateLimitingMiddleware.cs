using System.Collections.Concurrent;
using LearnPath.API.Common;

namespace LearnPath.API.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;

    private static readonly ConcurrentDictionary<string, (int Count, DateTime WindowStart)>
        _requests = new();

    private const int MAX_REQUESTS = 50;
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

        // OPTIONS preflight is CORS negotiation, not a user action — do not count against rate budget
        if (HttpMethods.IsOptions(context.Request.Method))
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

            // Use same envelope as the rest of the API so frontend can show correct message for 429
            // (not "Path Not Found"). Do not retry automatically — let UI show distinct rate-limit error.
            var body = ApiResponse<object>.Fail("Too many requests. Please wait a few seconds and try again.");
            await context.Response.WriteAsJsonAsync(body);

            return;
        }

        await _next(context);
    }
}

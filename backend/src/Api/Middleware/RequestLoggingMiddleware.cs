using System.Diagnostics;
using System.Security.Claims;

namespace Api.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestId = context.TraceIdentifier;
        var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var method = context.Request.Method;
        var path = context.Request.Path;
        var queryString = context.Request.QueryString.ToString();

        _logger.LogInformation("Starting request {RequestId} {Method} {Path}{QueryString} for user {UserId}",
            requestId, method, path, queryString, userId ?? "Anonymous");

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var statusCode = context.Response.StatusCode;
            var elapsed = stopwatch.ElapsedMilliseconds;

            if (statusCode >= 400)
            {
                _logger.LogWarning("Completed request {RequestId} {Method} {Path} with status {StatusCode} in {Elapsed}ms",
                    requestId, method, path, statusCode, elapsed);
            }
            else
            {
                _logger.LogInformation("Completed request {RequestId} {Method} {Path} with status {StatusCode} in {Elapsed}ms",
                    requestId, method, path, statusCode, elapsed);
            }
        }
    }
}
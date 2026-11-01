using Serilog.Context;

namespace AttendanceManagement.Services.Identity.Api.Middleware;

/// <summary>
/// Intercepts incoming HTTP requests to ensure a consistent X-Correlation-ID header exists.
/// Enriches Serilog logging context with this ID for distributed end-to-end request tracing.
/// </summary>
public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Check if upstream client or Ocelot gateway forwarded a correlation ID
        if (!context.Request.Headers.TryGetValue(CorrelationIdHeader, out var correlationId) ||
            string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
        }

        // 2. Attach to response headers so client can cross-reference logs
        context.Response.Headers.TryAdd(CorrelationIdHeader, correlationId);

        // 3. Push to Serilog LogContext for all logs generated within this HTTP request lifecycle
        using (LogContext.PushProperty("CorrelationId", correlationId.ToString()))
        {
            await _next(context);
        }
    }
}

using Serilog.Context;

namespace AttendanceManagement.Services.Attendance.Api.Middleware;

/// <summary>
/// Intercepts incoming HTTP requests to ensure a consistent X-Correlation-ID header exists.
/// Enriches Serilog logging context with this ID for distributed request tracing.
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
        if (!context.Request.Headers.TryGetValue(CorrelationIdHeader, out var correlationId) ||
            string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
        }

        context.Response.Headers.TryAdd(CorrelationIdHeader, correlationId);

        using (LogContext.PushProperty("CorrelationId", correlationId.ToString()))
        {
            await _next(context);
        }
    }
}

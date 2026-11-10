using System.Net;
using System.Text.Json;
using AttendanceManagement.Services.Attendance.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AttendanceManagement.Services.Attendance.Middleware;

/// <summary>
/// Catches unhandled exceptions across the HTTP pipeline and writes standardized RFC 7807 ProblemDetails responses.
/// Maps EF Core DbUpdateConcurrencyException to HTTP 409 Conflict for graceful optimistic concurrency handling.
/// Maps ServiceUnavailableException to HTTP 503 Service Unavailable for graceful integration fault handling.
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}: {Message}", context.Request.Method, context.Request.Path, ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        var (statusCode, problemDetails) = exception switch
        {
            ValidationException valEx => (
                HttpStatusCode.BadRequest,
                (ProblemDetails)new ValidationProblemDetails(valEx.Errors)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Validation Error",
                    Detail = "One or more validation rules failed for the request payload.",
                    Instance = context.Request.Path
                }
            ),

            NotFoundException notFoundEx => (
                HttpStatusCode.NotFound,
                new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Resource Not Found",
                    Detail = notFoundEx.Message,
                    Instance = context.Request.Path
                }
            ),

            ConflictException conflictEx => (
                HttpStatusCode.Conflict,
                new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Conflict",
                    Detail = conflictEx.Message,
                    Instance = context.Request.Path
                }
            ),

            DbUpdateConcurrencyException => (
                HttpStatusCode.Conflict,
                new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Concurrency Conflict",
                    Detail = "A concurrency conflict occurred. The record was modified by another operation. Please refresh and try again.",
                    Instance = context.Request.Path
                }
            ),

            ServiceUnavailableException serviceEx => (
                HttpStatusCode.ServiceUnavailable,
                new ProblemDetails
                {
                    Status = StatusCodes.Status503ServiceUnavailable,
                    Title = "Service Unavailable",
                    Detail = serviceEx.Message,
                    Instance = context.Request.Path
                }
            ),

            DomainException domainEx => (
                HttpStatusCode.UnprocessableEntity,
                new ProblemDetails
                {
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Title = "Business Rule Violation",
                    Detail = domainEx.Message,
                    Instance = context.Request.Path
                }
            ),

            UnauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Unauthorized",
                    Detail = "You are not authorized to perform this operation.",
                    Instance = context.Request.Path
                }
            ),

            _ => (
                HttpStatusCode.InternalServerError,
                new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Internal Server Error",
                    Detail = "An unexpected error occurred while processing your request.",
                    Instance = context.Request.Path
                }
            )
        };

        context.Response.StatusCode = (int)statusCode;
        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        string json = JsonSerializer.Serialize(problemDetails, problemDetails.GetType(), jsonOptions);
        await context.Response.WriteAsync(json);
    }
}

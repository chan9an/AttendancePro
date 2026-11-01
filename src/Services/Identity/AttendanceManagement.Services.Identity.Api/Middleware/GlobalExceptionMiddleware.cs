using System.Net;
using System.Text.Json;
using AttendanceManagement.Services.Identity.Application.Common.Exceptions;
using AttendanceManagement.Services.Identity.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceManagement.Services.Identity.Api.Middleware;

/// <summary>
/// Global exception handling middleware converting exceptions to standard RFC 7807 ProblemDetails responses.
/// Prevents internal stack traces from leaking to external callers.
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
            _logger.LogError(ex, "An unhandled exception occurred during request execution: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        var (statusCode, problemDetails) = exception switch
        {
            ValidationException validationEx => (
                HttpStatusCode.BadRequest,
                new ValidationProblemDetails(validationEx.Errors)
                {
                    Status = (int)HttpStatusCode.BadRequest,
                    Title = "Validation Error",
                    Detail = "One or more validation rules failed for the request payload.",
                    Instance = context.Request.Path
                }
            ),

            NotFoundException notFoundEx => (
                HttpStatusCode.NotFound,
                new ProblemDetails
                {
                    Status = (int)HttpStatusCode.NotFound,
                    Title = "Resource Not Found",
                    Detail = notFoundEx.Message,
                    Instance = context.Request.Path
                }
            ),

            IdentityDomainException domainEx => (
                HttpStatusCode.UnprocessableEntity,
                new ProblemDetails
                {
                    Status = (int)HttpStatusCode.UnprocessableEntity,
                    Title = "Domain Rule Violation",
                    Detail = domainEx.Message,
                    Instance = context.Request.Path
                }
            ),

            UnauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                new ProblemDetails
                {
                    Status = (int)HttpStatusCode.Unauthorized,
                    Title = "Unauthorized",
                    Detail = "You are not authorized to perform this operation.",
                    Instance = context.Request.Path
                }
            ),

            _ => (
                HttpStatusCode.InternalServerError,
                new ProblemDetails
                {
                    Status = (int)HttpStatusCode.InternalServerError,
                    Title = "Internal Server Error",
                    Detail = "An unexpected error occurred while processing your request. Please reference the correlation ID.",
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

using AttendanceManagement.Services.Identity.Api.Extensions;
using AttendanceManagement.Services.Identity.Api.Middleware;
using Serilog;

// 1. Initialize Serilog Structured Logger
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .Enrich.FromLogContext()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Identity & Employee Service API Host...");

    var builder = WebApplication.CreateBuilder(args);

    // 2. Configure Serilog with Host
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}"));

    // 3. Register Controllers
    builder.Services.AddControllers();

    // 4. Register Infrastructure, Identity, Services, and JWT Authentication
    builder.Services.AddIdentityInfrastructure(builder.Configuration);

    // 5. Register Swagger / OpenAPI
    builder.Services.AddSwaggerDocumentation();

    // 6. Build WebApplication
    var app = builder.Build();

    // 7. Configure Middleware Pipeline
    // Correlation ID must be the very first middleware to capture all subsequent log lines
    app.UseMiddleware<CorrelationIdMiddleware>();

    // Global Exception Middleware to format all errors as RFC 7807 ProblemDetails
    app.UseMiddleware<GlobalExceptionMiddleware>();

    // Swagger UI
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Identity & Employee API v1");
            c.RoutePrefix = string.Empty; // Serves Swagger UI at application root (http://localhost:5001/)
        });
    }

    app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Identity & Employee Service API terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}

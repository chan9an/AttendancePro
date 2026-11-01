using AttendanceManagement.Services.Attendance.Api.Extensions;
using AttendanceManagement.Services.Attendance.Api.Middleware;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .Enrich.FromLogContext()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Attendance & Leave Service API Host...");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}"));

    builder.Services.AddControllers();

    // Register Infrastructure, EF Core, Typed HttpClient, and JWT Security
    builder.Services.AddAttendanceInfrastructure(builder.Configuration);

    // Register Swagger / OpenAPI
    builder.Services.AddSwaggerDocumentation();

    var app = builder.Build();

    // Middleware Pipeline
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<GlobalExceptionMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Attendance & Leave API v1");
            c.RoutePrefix = string.Empty; // Serves Swagger at application root (http://localhost:5002/)
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
    Log.Fatal(ex, "Attendance & Leave Service API terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}

using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .Enrich.FromLogContext()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Ocelot API Gateway Host on Port 5000...");

    var builder = WebApplication.CreateBuilder(args);

    // 1. Configure Serilog structured logging
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] [Gateway] {Message:lj}{NewLine}{Exception}"));

    // 2. Load Ocelot Configuration File
    builder.Configuration.AddJsonFile("Configuration/ocelot.json", optional: false, reloadOnChange: true);

    // 3. Configure Ingress JWT Bearer Authentication
    // Validates token signature, issuer, audience, and expiration at Gateway ingress
    // Raw Authorization: Bearer token is forwarded downstream for independent microservice validation
    string secretKey = builder.Configuration["JwtSettings:Secret"] 
        ?? "EnterpriseSecretKeyForDevPurposesAttendanceLeaveManagement2026Min32Bytes!";
    string issuer = builder.Configuration["JwtSettings:Issuer"] ?? "AttendanceManagement.IdentityService";
    string audience = builder.Configuration["JwtSettings:Audience"] ?? "AttendanceManagement.Clients";

    builder.Services.AddAuthentication()
        .AddJwtBearer("GatewayBearer", options =>
        {
            options.RequireHttpsMetadata = false; // Dev environment
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

    // 4. Register Ocelot Gateway Services
    builder.Services.AddOcelot(builder.Configuration);

    // 5. Configure Swagger UI for unified downstream API exploration
    builder.Services.AddEndpointsApiExplorer();

    // 6. Configure CORS for client access
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAllClients", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });

    var app = builder.Build();

    app.UseCors("AllowAllClients");

    // 7. Correlation ID tracking middleware & Gateway Health Probe
    app.Use(async (context, next) =>
    {
        const string correlationHeader = "X-Correlation-ID";
        if (!context.Request.Headers.TryGetValue(correlationHeader, out var correlationId) ||
            string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
            context.Request.Headers[correlationHeader] = correlationId;
        }

        context.Response.Headers.TryAdd(correlationHeader, correlationId);

        // Instant health probe for container/orchestrator monitoring
        if (context.Request.Path == "/health")
        {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"status\":\"Healthy\",\"gateway\":\"AttendanceManagement.Gateway\",\"port\":5000}");
            return;
        }

        await next();
    });

    // 8. Unified Swagger UI at /swagger
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/gateway/swagger/identity/v1/swagger.json", "Identity & Employee API v1 (Gateway)");
        c.SwaggerEndpoint("/gateway/swagger/attendance/v1/swagger.json", "Attendance & Leave API v1 (Gateway)");
        c.RoutePrefix = "swagger";
    });

    // 9. Execute Ingress Authentication & Ocelot Pipeline
    app.UseAuthentication();
    await app.UseOcelot();

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Ocelot API Gateway terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}

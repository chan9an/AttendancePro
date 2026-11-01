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

    // 3. Register Ocelot Services
    builder.Services.AddOcelot(builder.Configuration);

    // 4. Configure CORS for client access
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

    // 5. Correlation ID tracking middleware
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
        await next();
    });

    // 6. Execute Ocelot Pipeline
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

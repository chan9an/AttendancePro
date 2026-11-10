using System.Text;
using AttendanceService.Clients;
using AttendanceService.Data;
using AttendanceService.Extensions;
using AttendanceService.Middleware;
using AttendanceService.Repositories;
using AttendanceService.Services;
using AttendanceService.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

// 1. Initialize Serilog bootstrap logger
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .Enrich.FromLogContext()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Attendance Service API Host...");

    var builder = WebApplication.CreateBuilder(args);

    // 2. Configure Serilog structured logging
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}"));

    // 3. Register Controllers
    builder.Services.AddControllers();

    // 4. Configure EF Core & SQL Server
    string connectionString = builder.Configuration.GetConnectionString("AttendanceConnection")
        ?? "Server=localhost,1433;Database=AttendanceManagement_AttendanceDb;User Id=sa;Password=EnterprisePassword123!;TrustServerCertificate=True;";

    builder.Services.AddDbContext<AttendanceDbContext>(options =>
        options.UseSqlServer(connectionString, sql =>
        {
            sql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
        }));

    // 5. Configure Typed HttpClient for Identity Integration
    string identityUrl = builder.Configuration["ServiceEndpoints:IdentityServiceBaseUrl"] ?? "http://localhost:5001";
    builder.Services.AddHttpClient<IIdentityIntegrationService, IdentityIntegrationService>(client =>
    {
        client.BaseAddress = new Uri(identityUrl);
        client.Timeout = TimeSpan.FromSeconds(10);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    });

    // 6. Register Application Repositories & Services
    builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();
    builder.Services.AddScoped<ILeaveRepository, LeaveRepository>();
    builder.Services.AddScoped<IProjectRepository, ProjectRepository>();

    builder.Services.AddScoped<IAttendanceService, AttendanceService>();
    builder.Services.AddScoped<ILeaveService, LeaveService>();
    builder.Services.AddScoped<ILeaveCreditService, LeaveCreditService>();
    builder.Services.AddScoped<IProjectService, ProjectService>();

    // 7. Register FluentValidation Validators
    builder.Services.AddValidatorsFromAssemblyContaining<CreateAttendanceValidator>();

    // 8. Configure Zero-Trust JWT Bearer Authentication (Independent Signature Validation)
    string secretKey = builder.Configuration["JwtSettings:Secret"] 
        ?? "EnterpriseSecretKeyForDevPurposesAttendanceLeaveManagement2026Min32Bytes!";
    string issuer = builder.Configuration["JwtSettings:Issuer"] ?? "AttendanceManagement.IdentityService";
    string audience = builder.Configuration["JwtSettings:Audience"] ?? "AttendanceManagement.Clients";

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
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

    // 9. Configure Authorization Policies
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Administrator"));
        options.AddPolicy("RequireManagerRole", policy => policy.RequireRole("Administrator", "Manager"));
        options.AddPolicy("ActiveEmployeeOnly", policy => policy.RequireClaim("employee_id"));
    });

    // 10. Configure Swagger / OpenAPI
    builder.Services.AddSwaggerDocumentation();

    var app = builder.Build();

    // 11. Middleware Pipeline
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<GlobalExceptionMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Attendance Service API v1");
            c.RoutePrefix = string.Empty; // Serves Swagger UI at root (http://localhost:5002/)
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
    Log.Fatal(ex, "Attendance Service API host terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}

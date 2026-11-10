using System.Text;
using AttendanceManagement.Services.Identity.Data;
using AttendanceManagement.Services.Identity.Entities;
using AttendanceManagement.Services.Identity.Extensions;
using AttendanceManagement.Services.Identity.Middleware;
using AttendanceManagement.Services.Identity.Services;
using AttendanceManagement.Services.Identity.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
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
    Log.Information("Starting Identity & Employee Service API Host...");

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
    string connectionString = builder.Configuration.GetConnectionString("IdentityConnection")
        ?? "Server=localhost,1433;Database=AttendanceManagement_IdentityDb;User Id=sa;Password=EnterprisePassword123!;TrustServerCertificate=True;";

    builder.Services.AddDbContext<IdentityDbContext>(options =>
        options.UseSqlServer(connectionString, sql =>
        {
            sql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
        }));

    // 5. Configure ASP.NET Core Identity
    builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.User.RequireUniqueEmail = true;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddEntityFrameworkStores<IdentityDbContext>()
    .AddDefaultTokenProviders();

    // 6. Register Application Services
    builder.Services.AddScoped<ITokenService, TokenService>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IEmployeeService, EmployeeService>();

    // 7. Register FluentValidation Validators
    builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

    // 8. Configure JWT Bearer Authentication
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
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Identity & Employee API v1");
            c.RoutePrefix = string.Empty; // Serves Swagger UI at root (http://localhost:5001/)
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
    Log.Fatal(ex, "Identity & Employee Service API host terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}

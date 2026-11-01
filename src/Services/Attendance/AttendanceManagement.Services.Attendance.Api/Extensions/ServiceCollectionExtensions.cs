using System.Text;
using AttendanceManagement.Services.Attendance.Application.Common.Interfaces;
using AttendanceManagement.Services.Attendance.Application.Contracts;
using AttendanceManagement.Services.Attendance.Application.Services;
using AttendanceManagement.Services.Attendance.Application.Validators;
using AttendanceManagement.Services.Attendance.Infrastructure.Clients;
using AttendanceManagement.Services.Attendance.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace AttendanceManagement.Services.Attendance.Api.Extensions;

/// <summary>
/// Registers Attendance service infrastructure, application services, typed HTTP clients, and JWT security.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAttendanceInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Configure EF Core with SQL Server
        string connectionString = configuration.GetConnectionString("AttendanceConnection")
            ?? "Server=localhost,1433;Database=AttendanceManagement_AttendanceDb;User Id=sa;Password=EnterprisePassword123!;TrustServerCertificate=True;";

        services.AddDbContext<AttendanceAppDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(AttendanceAppDbContext).Assembly.FullName);
                sqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
            }));

        services.AddScoped<IAttendanceAppDbContext>(provider => provider.GetRequiredService<AttendanceAppDbContext>());

        // 2. Configure Service Endpoints & Typed HttpClient
        services.Configure<ServiceEndpoints>(configuration.GetSection(ServiceEndpoints.SectionName));

        services.AddHttpClient<IIdentityIntegrationService, IdentityIntegrationService>((serviceProvider, client) =>
        {
            var endpoints = configuration.GetSection(ServiceEndpoints.SectionName).Get<ServiceEndpoints>() ?? new ServiceEndpoints();
            client.BaseAddress = new Uri(endpoints.IdentityServiceBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        // 3. Register Application Services
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<ILeaveService, LeaveService>();
        services.AddScoped<ILeaveCreditService, LeaveCreditService>();

        // 4. Register FluentValidation
        services.AddValidatorsFromAssemblyContaining<CreateAttendanceValidator>();

        // 5. Configure JWT Bearer Authentication (Independent Zero-Trust Verification)
        string secretKey = configuration["JwtSettings:Secret"] ?? "EnterpriseSecretKeyForDevPurposesAttendanceLeaveManagement2026Min32Bytes!";
        string issuer = configuration["JwtSettings:Issuer"] ?? "AttendanceManagement.IdentityService";
        string audience = configuration["JwtSettings:Audience"] ?? "AttendanceManagement.Clients";

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
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

        // 6. Authorization Policies
        services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Administrator"));
            options.AddPolicy("RequireManagerRole", policy => policy.RequireRole("Administrator", "Manager"));
            options.AddPolicy("ActiveEmployeeOnly", policy => policy.RequireClaim("employee_id"));
        });

        return services;
    }
}

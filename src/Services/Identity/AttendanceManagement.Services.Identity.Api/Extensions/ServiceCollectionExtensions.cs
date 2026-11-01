using System.Text;
using AttendanceManagement.Services.Identity.Application.Common.Interfaces;
using AttendanceManagement.Services.Identity.Application.Contracts;
using AttendanceManagement.Services.Identity.Application.Services;
using AttendanceManagement.Services.Identity.Application.Validators;
using AttendanceManagement.Services.Identity.Infrastructure.Persistence;
using AttendanceManagement.Services.Identity.Infrastructure.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace AttendanceManagement.Services.Identity.Api.Extensions;

/// <summary>
/// Configures dependency injection registrations for Identity Service layers.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Configure EF Core with SQL Server
        string connectionString = configuration.GetConnectionString("IdentityConnection") 
            ?? "Server=localhost,1433;Database=AttendanceManagement_IdentityDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True;";

        services.AddDbContext<IdentityAppDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(IdentityAppDbContext).Assembly.FullName);
                sqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
            }));

        services.AddScoped<IIdentityAppDbContext>(provider => provider.GetRequiredService<IdentityAppDbContext>());

        // 2. Configure ASP.NET Core Identity
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
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
        .AddEntityFrameworkStores<IdentityAppDbContext>()
        .AddDefaultTokenProviders();

        // 3. Register Application Services
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmployeeService, EmployeeService>();

        // 4. Register FluentValidation Validators
        services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

        // 5. Configure JWT Bearer Authentication
        string secretKey = configuration["JwtSettings:Secret"] ?? "EnterpriseSecretKeyForDevPurposesAttendanceLeaveManagement2026";
        string issuer = configuration["JwtSettings:Issuer"] ?? "AttendanceManagement.IdentityService";
        string audience = configuration["JwtSettings:Audience"] ?? "AttendanceManagement.Clients";

        services.AddAuthentication(options =>
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

        // 6. Role & Claim-Based Authorization Policies
        services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Administrator"));
            options.AddPolicy("RequireManagerRole", policy => policy.RequireRole("Administrator", "Manager"));
            options.AddPolicy("ActiveEmployeeOnly", policy => policy.RequireClaim("employee_id"));
        });

        return services;
    }
}

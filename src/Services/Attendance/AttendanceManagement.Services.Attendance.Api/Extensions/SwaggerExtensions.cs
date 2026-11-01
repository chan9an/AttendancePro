using Microsoft.OpenApi.Models;

namespace AttendanceManagement.Services.Attendance.Api.Extensions;

/// <summary>
/// Configures OpenAPI/Swagger documentation with JWT Bearer authentication scheme and XML comments.
/// </summary>
public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Attendance & Leave Management — Attendance & Leave Service API",
                Version = "v1",
                Description = "Microservice managing daily attendance, auto-approval workflows, leave requests, and transactional leave ledgers.",
                Contact = new OpenApiContact
                {
                    Name = "Enterprise Architecture Team",
                    Email = "architecture@enterprise.com"
                }
            });

            string xmlFile = $"{typeof(Program).Assembly.GetName().Name}.xml";
            string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }

            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Enter JWT Bearer token format: Bearer {your_token_here}",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Reference = new OpenApiReference
                {
                    Id = "Bearer",
                    Type = ReferenceType.SecurityScheme
                }
            };

            options.AddSecurityDefinition("Bearer", securityScheme);

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                { securityScheme, Array.Empty<string>() }
            });
        });

        return services;
    }
}

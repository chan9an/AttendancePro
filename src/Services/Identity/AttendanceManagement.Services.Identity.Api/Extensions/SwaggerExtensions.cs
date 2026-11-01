using Microsoft.OpenApi.Models;

namespace AttendanceManagement.Services.Identity.Api.Extensions;

/// <summary>
/// Configures OpenAPI/Swagger generation with JWT Bearer authentication scheme and XML comments.
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
                Title = "Attendance & Leave Management — Identity & Employee Service API",
                Version = "v1",
                Description = "Microservice managing enterprise authentication, JWT security tokens, and employee directory master data.",
                Contact = new OpenApiContact
                {
                    Name = "Enterprise Architecture Team",
                    Email = "architecture@enterprise.com"
                }
            });

            // Include XML comments if file exists
            string xmlFile = $"{typeof(Program).Assembly.GetName().Name}.xml";
            string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }

            // Define JWT Bearer security scheme in Swagger
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

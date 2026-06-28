using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LearnPath.API.Swagger;

public static class SwaggerConfig
{
    public static IServiceCollection AddSwaggerConfiguration(
        this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title       = "LearnPath API",
                Version     = "v1",
                Description = """
                    ## LearnPath — Graph-Based Learning Platform API

                    ### Authentication
                    Use the **Authorize** button and enter: `Bearer {your_jwt_token}`

                    ### Rate Limiting
                    100 requests per minute per IP address.

                    ### Response Format
                    All responses follow: `{ success, message, data, errors, timestamp }`
                    """,
                Contact = new OpenApiContact
                {
                    Name  = "LearnPath Team",
                    Email = "support@learnpath.dev",
                },
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name         = "Authorization",
                Type         = SecuritySchemeType.Http,
                Scheme       = "bearer",
                BearerFormat = "JWT",
                In           = ParameterLocation.Header,
                Description  = "JWT token. Example: `Bearer eyJhbGci...`",
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id   = "Bearer",
                        }
                    },
                    Array.Empty<string>()
                }
            });

            options.TagActionsBy(api =>
            {
                if (api.GroupName != null) return [api.GroupName];
                if (api.ActionDescriptor is
                    Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor desc)
                    return [desc.ControllerName];
                return ["Other"];
            });

            options.DocInclusionPredicate((_, _) => true);
        });

        return services;
    }
}

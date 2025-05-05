using Asp.Versioning.ApiExplorer;
using Microsoft.OpenApi.Models;

namespace BambooCard.WebAPI.Extensions;

public static class SwaggerGenExtensions
{
    public static IServiceCollection AddSwaggerGenWithVersioning(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            var provider = services.BuildServiceProvider().GetRequiredService<IApiVersionDescriptionProvider>();

            foreach (var desc in provider.ApiVersionDescriptions)
            {
                options.SwaggerDoc(desc.GroupName, new OpenApiInfo
                {
                    Title = $"My API {desc.ApiVersion}",
                    Version = desc.ApiVersion.ToString(),
                    Description = desc.IsDeprecated ? "Deprecated API version" : null
                });
            }
        });

        return services;
    }
}
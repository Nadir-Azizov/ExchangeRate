using Asp.Versioning.ApiExplorer;

namespace BambooCard.WebAPI.Extensions;

public static class SwaggerUIExtensions
{
    public static IApplicationBuilder UseSwaggerUIWithVersioning(this IApplicationBuilder app)
    {
        app.UseSwaggerUI(opts =>
        {
            var provider = app.ApplicationServices.GetRequiredService<IApiVersionDescriptionProvider>();

            foreach (var desc in provider.ApiVersionDescriptions)
            {
                opts.SwaggerEndpoint($"/swagger/{desc.GroupName}/swagger.json", desc.GroupName);
            }
        });

        return app;
    }
}

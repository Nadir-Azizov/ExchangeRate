using BambooCard.WebAPI.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BambooCard.WebAPI.Extensions;
public static class HealthCheckExtensions
{
    public static IServiceCollection AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var hcBuilder = services.AddHealthChecks();

        hcBuilder.AddCheck(
            "self",
            () => HealthCheckResult.Healthy(),
            tags: ["live"]
        );

        hcBuilder.AddCheck<SqlServerHealthCheck>(
            name: "SQL Server",
            failureStatus: null,
            tags: ["ready", "db"]
        );

        return services;
    }

    public static IEndpointRouteBuilder MapHealthCheckLive(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live")
        });
        return endpoints;
    }

    public static IEndpointRouteBuilder MapHealthCheckReady(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = async (ctx, report) =>
            {
                ctx.Response.ContentType = "application/json";
                var payload = new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        description = e.Value.Description
                    })
                };
                await ctx.Response.WriteAsJsonAsync(payload);
            }
        });
        return endpoints;
    }
}
using BambooCard.Infrastructure.Abstractions;
using BambooCard.Infrastructure.Implementions;
using BambooCard.Infrastructure.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.RateLimiting;

namespace BambooCard.Infrastructure.Statics;

public static class ServiceRegistration
{
    public static IServiceCollection AddInfrastructureDependencies(this IServiceCollection services, ConfigurationManager configuration)
    {
        #region ======== Cache ========

        services.AddMemoryCache();
        services.AddScoped<ICacheManager, MemoryCacheManager>();

        #endregion ======== Cache ========


        services.Configure<CacheSettings>(configuration.GetSection(nameof(CacheSettings)));
        services.Configure<RetryPolicySettings>(configuration.GetSection(nameof(RetryPolicySettings)));
        services.Configure<OpenTelemetrySettings>(configuration.GetSection(nameof(OpenTelemetrySettings)));


        #region ======== Rate Limiting ========

        var incoming = configuration.GetSection(nameof(RetryPolicySettings)).Get<RetryPolicySettings>();

        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
            {
                var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: ip,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = incoming.PermitLimit,
                        Window = TimeSpan.FromSeconds(incoming.WindowSeconds),
                        QueueLimit = incoming.QueueLimit,
                        AutoReplenishment = true
                    });
            });

            options.OnRejected = async (ctx, ct) =>
            {
                ctx.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await ctx.HttpContext.Response.WriteAsync("Too many requests — please wait.", ct);
            };
        });

        #endregion ======== Rate Limiting ========

        return services;
    }
}

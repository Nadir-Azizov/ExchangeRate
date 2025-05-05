using BambooCard.Business.Abstractions;
using BambooCard.Business.Automation;
using BambooCard.Business.Managers;
using BambooCard.Business.Mapping;
using BambooCard.Infrastructure.Enums;
using BambooCard.Infrastructure.Helper;
using MapsterMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace BambooCard.Business.Statics;

public static class ServiceRegistration
{
    public static IServiceCollection AddBusinessDependencies(this IServiceCollection services, ConfigurationManager configuration)
    {
        services
            .AddScoped<IExchangeManager, ExchangeManager>()
            .AddScoped<IAuthManager, AuthManager>();

        #region ========== Mapster ========== 

        MapsterConfiguration.ConfigureMappings();
        services.AddSingleton<IMapper, Mapper>();

        #endregion ========== Mapster ========== 


        #region ========== Automation ========== 

        services.AddQuartz(q =>
        {
            var jobKey = KeyHelper.GetJobKey(EKeyType.ExchangeLatest);
            q.AddJob<FetchLatestExchangeRates>(j => j.WithIdentity(jobKey));

            q.AddTrigger(t => t
                .ForJob(jobKey)
                .WithIdentity("ExchangeRates-Trigger")
                .WithCronSchedule(
                    "0 1 16 ? * MON-FRI",
                    x => x.InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"))
                )
            );
        });

        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
        #endregion ========== Automation ========== 


        return services;
    }
}

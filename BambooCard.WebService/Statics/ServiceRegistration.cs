using BambooCard.Infrastructure.Helper;
using BambooCard.Infrastructure.Settings;
using BambooCard.WebService.Abstractions;
using BambooCard.WebService.Implementions;
using BambooCard.WebService.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace BambooCard.WebService.Statics;

public static class ServiceRegistration
{
    public static IServiceCollection AddWebServiceDependencies(this IServiceCollection services, ConfigurationManager configuration)
    {
        services.Configure<ExchangeProviderSettings>(configuration.GetSection(nameof(ExchangeProviderSettings)));


        services.AddSingleton(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<RetryPolicySettings>>().Value;
            var logger = sp.GetRequiredService<ILogger<FrankfurterProvider>>();

            return RetryHelper.CreateResiliencePolicy(
                retryCount: settings.RetryMaxAttempts,
                baseDelaySeconds: settings.RetryDelaySeconds,
                allowedFailuresBeforeBreak: settings.AllowedFailuresBeforeBreak,
                breakDuration: TimeSpan.FromSeconds(settings.BreakDuration),
                logger: logger
            );
        });

        services
          .AddHttpClient<FrankfurterProvider>((sp, client) =>
          {
              var url = sp.GetRequiredService<IOptions<ExchangeProviderSettings>>()
                          .Value.FrankfurterLatestRateUrl;
              client.BaseAddress = new Uri(url);
          })
          .AddPolicyHandler((serviceProvider, request) =>
              serviceProvider.GetRequiredService<IAsyncPolicy<HttpResponseMessage>>());


        services
            .AddScoped<ICurrencyProvider, FrankfurterProvider>()
            .AddScoped<ICurrencyProviderFactory, CurrencyProviderFactory>();


        return services;
    }
}
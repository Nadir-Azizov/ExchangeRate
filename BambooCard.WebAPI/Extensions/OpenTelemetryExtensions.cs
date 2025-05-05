using Azure.Monitor.OpenTelemetry.Exporter;
using BambooCard.Infrastructure.Settings;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace BambooCard.WebAPI.Extensions;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddCustomOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var telemetry = configuration.GetSection(nameof(OpenTelemetrySettings)).Get<OpenTelemetrySettings>();

        services.AddOpenTelemetry()
            .WithTracing(otel =>
            {
                otel
                  .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("BambooCard.WebAPI"))
                  .AddAspNetCoreInstrumentation(opts => opts.RecordException = true)
                  .AddHttpClientInstrumentation();

                if (telemetry?.EnableSqlClientInstrumentation == true)
                {
                    otel.AddSqlClientInstrumentation(options => options.SetDbStatementForText = true);
                    otel.AddEntityFrameworkCoreInstrumentation();
                }

                if (!string.IsNullOrEmpty(telemetry?.ConnectionString))
                {
                    otel.AddAzureMonitorTraceExporter(o => o.ConnectionString = telemetry.ConnectionString);
                }
                else
                {
                    otel.AddConsoleExporter();
                }
            });

        return services;
    }
}

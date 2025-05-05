using BambooCard.Business.Abstractions;
using BambooCard.Infrastructure.Enums;
using Microsoft.Extensions.Logging;
using Quartz;

namespace BambooCard.Business.Automation;

[DisallowConcurrentExecution]
public class FetchLatestExchangeRates(
    IExchangeManager exchangeManager,
    ILogger<FetchLatestExchangeRates> logger
    ) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            await exchangeManager.ImportLatestAsync(EProvider.Frankfurter);

            logger.LogInformation("Exchange rates are fetched.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching exchange rates");
        }
    }
}
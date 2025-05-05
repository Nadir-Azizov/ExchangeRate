using BambooCard.Infrastructure.Enums;
using BambooCard.WebService.Models;

namespace BambooCard.WebService.Abstractions;

public interface ICurrencyProvider
{
    EProvider Provider { get; }
    Task<ExchangeRateDto> FetchLatestAsync(CancellationToken cancellationToken = default);
}

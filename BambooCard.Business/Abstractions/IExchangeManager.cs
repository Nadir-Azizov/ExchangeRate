using BambooCard.Business.Models.Main;
using BambooCard.Domain.Enums;
using BambooCard.Infrastructure.Enums;
using BambooCard.Infrastructure.Results;

namespace BambooCard.Business.Abstractions;

public interface IExchangeManager
{
    Task<ExchangeRateDto> GetServiceRateAsync(EProvider provider);
    Task<ExchangeRateDto> GetCurrentRateAsync();
    Task<PaginationResult<ExchangeRateDto>> SearchRatesAsync(ExchangeSearchModel model);
    Task<Dictionary<ECurrency, decimal>> ConvertToAllAsync(ECurrency fromCurrency, decimal amount);
    Task RefreshCacheFromDbAsync();
    Task<ExchangeRateDto> ImportLatestAsync(EProvider provider);
}

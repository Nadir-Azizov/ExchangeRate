using BambooCard.Business.Abstractions;
using BambooCard.Business.Extensions;
using BambooCard.Business.Filters;
using BambooCard.Business.Models.Main;
using BambooCard.Domain.Abstractions;
using BambooCard.Domain.Entities.Main;
using BambooCard.Domain.Enums;
using BambooCard.Infrastructure.Abstractions;
using BambooCard.Infrastructure.Enums;
using BambooCard.Infrastructure.Exceptions;
using BambooCard.Infrastructure.Helper;
using BambooCard.Infrastructure.Results;
using BambooCard.Infrastructure.Settings;
using BambooCard.WebService.Abstractions;
using Mapster;
using Microsoft.Extensions.Options;

namespace BambooCard.Business.Managers;

public class ExchangeManager(
    ICurrencyProviderFactory providerFactory,
    ICacheManager cache,
    IExchangeRateRepository exchangeRateRepository,
    IOptions<CacheSettings> cacheSettings
    ) : IExchangeManager
{
    public async Task<ExchangeRateDto> GetServiceRateAsync(EProvider provider)
    {
        var data = providerFactory.GetProvider(provider);

        var result = await data.FetchLatestAsync(CancellationToken.None);

        return result.Adapt<ExchangeRateDto>();
    }

    public async Task<ExchangeRateDto> GetCurrentRateAsync()
    {
        var cacheKey = KeyHelper.GetCacheKey(EKeyType.ExchangeLatest);
        if (cache.IsAdd(cacheKey))
            return cache.Get<ExchangeRateDto>(cacheKey);

        var data = await GetLatestRateFromDb()
            ?? throw new NotFoundException("Exchange rate not found");

        cache.Add(cacheKey, data, cacheSettings.Value.Duration);

        return data;
    }

    public async Task<PaginationResult<ExchangeRateDto>> SearchRatesAsync(ExchangeSearchModel model)
    {
        return await exchangeRateRepository.GetAllWithRates()
            .Filter(model)
            .ToPagedResultAsync<ExchangeRate, ExchangeRateDto>(
                model.PageNumber,
                model.PageSize
            );
    }

    public async Task<Dictionary<ECurrency, decimal>> ConvertToAllAsync(ECurrency fromCurrency, decimal amount)
    {
        var dto = await GetCurrentRateAsync();
        var rates = dto.Rates;
        var baseCurrency = dto.BaseCurrency;

        decimal fromRate = fromCurrency == baseCurrency
            ? 1m
            : rates.TryGetValue(fromCurrency, out var r)
                ? r
                : throw new BadRequestException($"No rate for {fromCurrency}");

        var amountInBase = amount / fromRate;

        var result = rates
            .Where(kvp => kvp.Key != fromCurrency)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => Math.Round(amountInBase * kvp.Value, 4)
            );

        result[baseCurrency] = Math.Round(amountInBase, 4);

        return result;
    }

    public async Task RefreshCacheFromDbAsync()
    {
        var latest = await GetLatestRateFromDb();

        RefreshCache(latest);
    }

    public async Task<ExchangeRateDto> ImportLatestAsync(EProvider provider)
    {
        var data = await GetServiceRateAsync(provider);

        var entity = data.Adapt<ExchangeRate>();

        await exchangeRateRepository.AddIfNotExists(entity);
        await exchangeRateRepository.SaveChangesAsync();

        var result = entity.Adapt<ExchangeRateDto>();

        RefreshCache(result);

        return result;
    }


    #region private

    private void RefreshCache(ExchangeRateDto data)
    {
        var cacheKey = KeyHelper.GetCacheKey(EKeyType.ExchangeLatest);

        cache.Remove(cacheKey);
        cache.Add(cacheKey, data, cacheSettings.Value.Duration);
    }

    private async Task<ExchangeRateDto> GetLatestRateFromDb()
    {
        var entity = await exchangeRateRepository.GetLatestExchangeRate()
            ?? throw new NotFoundException("Exchange rate not found");

        return entity.Adapt<ExchangeRateDto>();
    }

    #endregion private
}

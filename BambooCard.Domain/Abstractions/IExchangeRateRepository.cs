using BambooCard.Domain.Abstractions.Base;
using BambooCard.Domain.Entities.Main;

namespace BambooCard.Domain.Abstractions;

public interface IExchangeRateRepository : IRepository<ExchangeRate>
{
    Task<ExchangeRate> GetLatestExchangeRate();
    IQueryable<ExchangeRate> GetAllWithRates();
    Task<ExchangeRate> AddIfNotExists(ExchangeRate exchangeRate);
}

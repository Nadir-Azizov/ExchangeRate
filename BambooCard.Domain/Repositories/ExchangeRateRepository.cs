using BambooCard.Domain.Abstractions;
using BambooCard.Domain.Abstractions.Base;
using BambooCard.Domain.DbContext;
using BambooCard.Domain.Entities.Main;
using Microsoft.EntityFrameworkCore;

namespace BambooCard.Domain.Repositories;

public class ExchangeRateRepository(BambooCardDbContext context) : Repository<ExchangeRate>(context), IExchangeRateRepository
{
    public IQueryable<ExchangeRate> GetAllWithRates()
    {
        return context.ExchangeRates
             .Include(x => x.Rates)
             .AsQueryable();
    }

    public async Task<ExchangeRate> GetLatestExchangeRate()
    {
        return await context.ExchangeRates
            .Include(x => x.Rates)
            .OrderByDescending(x => x.Date)
            .FirstOrDefaultAsync();
    }

    public async Task<ExchangeRate> AddIfNotExists(ExchangeRate exchangeRate)
    {
        var exists = await context.ExchangeRates.FirstOrDefaultAsync(x => x.Date.Date == exchangeRate.Date.Date);
        if (exists != null)
            return exists;

        await context.ExchangeRates.AddAsync(exchangeRate);

        return exchangeRate;
    }
}

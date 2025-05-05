using BambooCard.Business.Models.Main;
using BambooCard.Domain.Entities.Main;
using BambooCard.Domain.Enums;

namespace BambooCard.Business.Filters;

public static class ExchangeSearchFilter
{
    public static IQueryable<ExchangeRate> Filter(this IQueryable<ExchangeRate> query, ExchangeSearchModel model)
    {
        if (model.BaseCurrency.HasValue)
            query = query.Where(x => x.BaseCurrency == model.BaseCurrency.Value);

        if (model.FromDate.HasValue)
            query = query.Where(x => model.FromDate.Value.Date <= x.Date.Date);

        if (model.ToDate.HasValue)
            query = query.Where(x => x.Date.Date <= model.ToDate.Value.Date);

        if (!string.IsNullOrWhiteSpace(model.Search))
        {
            if (Enum.TryParse<ECurrency>(model.Search, true, out var cur))
                query = query.Where(x =>
                    x.BaseCurrency == cur
                    || x.Rates.Any(r => r.Currency == cur));

            else if (decimal.TryParse(model.Search, out var val))
                query = query.Where(x => x.Rates.Any(r => r.Value == val));
        }

        return query;
    }
}

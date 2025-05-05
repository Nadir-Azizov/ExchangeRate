using BambooCard.Infrastructure.Enums;
using BambooCard.Infrastructure.Exceptions;
using BambooCard.WebService.Abstractions;

namespace BambooCard.WebService.Implementions;

public class CurrencyProviderFactory(IEnumerable<ICurrencyProvider> providers) : ICurrencyProviderFactory
{
    private readonly IDictionary<EProvider, ICurrencyProvider> providers = providers.ToDictionary(p => p.Provider, p => p);

    public ICurrencyProvider GetProvider(EProvider provider)
    {
        if (!providers.TryGetValue(provider, out var result))
            throw new NotFoundException($"No currency‐provider registered for '{provider}'");

        return result;
    }
}
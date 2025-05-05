using BambooCard.Infrastructure.Enums;

namespace BambooCard.WebService.Abstractions;

public interface ICurrencyProviderFactory
{
    /// <summary>
    /// Creates an instance of ICurrencyProvider based on the specified provider enum.
    /// </summary>
    ICurrencyProvider GetProvider(EProvider provider);
}

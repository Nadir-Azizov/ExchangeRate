using BambooCard.Business.Models.Main;
using BambooCard.Business.Models.User;
using BambooCard.Domain.Entities.Main;
using BambooCard.Domain.Entities.User;
using BambooCard.Domain.Enums;
using BambooCard.Infrastructure.Enums;
using Mapster;
using WebServices = BambooCard.WebService.Models;

namespace BambooCard.Business.Mapping;

public static class MapsterConfiguration
{
    public static void ConfigureMappings()
    {
        #region Main

        TypeAdapterConfig<WebServices.ExchangeRateDto, ExchangeRate>.NewConfig()
            .Map(dest => dest.BaseCurrency, src => Enum.Parse<ECurrency>(src.Base, true))
            .Map(dest => dest.Provider, src => EProvider.Frankfurter)
            .AfterMapping((src, dest) =>
            {
                dest.Rates = src.Rates.Select(kv => new Rate
                {
                    Currency = Enum.Parse<ECurrency>(kv.Key),
                    Value = kv.Value
                }).ToList();
            });

        TypeAdapterConfig<WebServices.ExchangeRateDto, ExchangeRateDto>.NewConfig()
            .Map(dest => dest.BaseCurrency, src => Enum.Parse<ECurrency>(src.Base, true))
            .Map(dest => dest.Rates,
                 src => src.Rates.ToDictionary(
                            kv => Enum.Parse<ECurrency>(kv.Key, true),
                            kv => kv.Value
                         )
            );

        TypeAdapterConfig<ExchangeRate, ExchangeRateDto>.NewConfig()
            .Map(dest => dest.Rates,
                 src => src.Rates.ToDictionary(kv => kv.Currency, kv => kv.Value)
            );

        TypeAdapterConfig<ExchangeRateDto, ExchangeRate>.NewConfig()
            .Map(dest => dest.Provider, src => EProvider.Frankfurter)
            .AfterMapping((src, dest) =>
            {
                dest.Rates = src.Rates.Select(kv => new Rate
                {
                    Currency = kv.Key,
                    Value = kv.Value
                }).ToList();
            });

        #endregion Main

        #region User

        TypeAdapterConfig<RegisterDto, AppUser>.NewConfig()
            .Map(dest => dest.UserName, src => src.Email);

        #endregion User
    }
}
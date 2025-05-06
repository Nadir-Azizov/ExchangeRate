using BambooCard.Business.Managers;
using BambooCard.Business.Models.Main;
using BambooCard.Domain.Abstractions;
using BambooCard.Domain.DbContext;
using BambooCard.Domain.Entities.Main;
using BambooCard.Domain.Enums;
using BambooCard.Domain.Repositories;
using BambooCard.Infrastructure.Abstractions;
using BambooCard.Infrastructure.Enums;
using BambooCard.Infrastructure.Results;
using BambooCard.Infrastructure.Settings;
using BambooCard.WebService.Abstractions;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NSubstitute;
using WebServiceExchangeDto = BambooCard.WebService.Models.ExchangeRateDto;

namespace BambooCard.Tests.Business.Managers;
public class ExchangeManagerConvertTests
{
    private readonly BambooCardDbContext _db;
    private readonly ExchangeManager _sut;
    private readonly ICacheManager _cache;
    private readonly IOptions<CacheSettings> _cacheSettings;
    private readonly ICurrencyProviderFactory _prov;

    public ExchangeManagerConvertTests()
    {
        var opts = new DbContextOptionsBuilder<BambooCardDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new BambooCardDbContext(opts);
        var repo = new ExchangeRateRepository(_db);

        _cache = new DummyCacheManager();
        _prov = Substitute.For<ICurrencyProviderFactory>();
        _cacheSettings = Options.Create(new CacheSettings { Duration = 5 });

        _sut = new ExchangeManager(_prov, _cache, repo, _cacheSettings);

        TypeAdapterConfig<WebServiceExchangeDto, ExchangeRate>
            .NewConfig()
            .Map(d => d.BaseCurrency, src => Enum.Parse<ECurrency>(src.Base, true))
            .Map(d => d.Provider, src => EProvider.Frankfurter)
            .AfterMapping((src, d) =>
            {
                d.Rates = src.Rates
                    .Select(kv => new Rate
                    {
                        Currency = Enum.Parse<ECurrency>(kv.Key, true),
                        Value = kv.Value
                    })
                    .ToList();
            });

        TypeAdapterConfig<WebServiceExchangeDto, ExchangeRateDto>
            .NewConfig()
            .Map(d => d.BaseCurrency, src => Enum.Parse<ECurrency>(src.Base, true))
            .Map(d => d.Rates,
                 src => src.Rates.ToDictionary(
                    kv => Enum.Parse<ECurrency>(kv.Key, true),
                    kv => kv.Value
                 ));

        TypeAdapterConfig<ExchangeRate, ExchangeRateDto>
            .NewConfig()
            .Map(d => d.Rates,
                 src => src.Rates.ToDictionary(r => r.Currency, r => r.Value));

        TypeAdapterConfig<ExchangeRateDto, ExchangeRate>
            .NewConfig()
            .Map(d => d.Provider, src => EProvider.Frankfurter)
            .AfterMapping((src, d) =>
            {
                d.Rates = src.Rates
                    .Select(kv => new Rate
                    {
                        Currency = kv.Key,
                        Value = kv.Value
                    })
                    .ToList();
            });
    }

    [Fact]
    public async Task ConvertToAllAsync_FromBaseCurrency_ShouldConvertOthers()
    {
        var entity = new ExchangeRate
        {
            BaseCurrency = ECurrency.USD,
            Rates = new List<Rate> {
                new Rate { Currency = ECurrency.EUR, Value = 2m },
                new Rate { Currency = ECurrency.GBP, Value = 3m }
            }
        };
        _db.ExchangeRates.Add(entity);
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.ConvertToAllAsync(ECurrency.USD, 5m);

        // Assert
        Assert.Equal(10m, result[ECurrency.EUR]);
        Assert.Equal(15m, result[ECurrency.GBP]);
        Assert.Equal(5m, result[ECurrency.USD]);
    }

    [Fact]
    public async Task ConvertToAllAsync_ToBaseCurrency_ShouldConvertOthers()
    {
        var entity = new ExchangeRate
        {
            BaseCurrency = ECurrency.USD,
            Rates = new List<Rate> {
                new Rate { Currency = ECurrency.EUR, Value = 2m },
                new Rate { Currency = ECurrency.GBP, Value = 3m }
            }
        };
        _db.ExchangeRates.Add(entity);
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.ConvertToAllAsync(ECurrency.EUR, 4m);

        // Assert
        Assert.Equal(6m, result[ECurrency.GBP]);
        Assert.Equal(2m, result[ECurrency.USD]);
    }

    private class DummyCacheManager : ICacheManager
    {
        public bool IsAdd(string key) => false;
        public void Remove(string key) { }
        public void Add(string key, object value, int duration) { }
        public T Get<T>(string key) => default;
        public object Get(string key) => null;
        public PaginationResult<T> GetPaginated<T>(string key, int pageIndex, int pageSize) => null;
        public void RemoveByPattern(string pattern) { }
    }
}

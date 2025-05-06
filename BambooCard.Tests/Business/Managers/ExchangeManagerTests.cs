using BambooCard.Business.Managers;
using BambooCard.Business.Models.Main;
using BambooCard.Domain.Abstractions;
using BambooCard.Domain.DbContext;
using BambooCard.Domain.Entities.Main;
using BambooCard.Domain.Enums;
using BambooCard.Domain.Repositories;
using BambooCard.Infrastructure.Abstractions;
using BambooCard.Infrastructure.Enums;
using BambooCard.Infrastructure.Exceptions;
using BambooCard.Infrastructure.Helper;
using BambooCard.Infrastructure.Results;
using BambooCard.Infrastructure.Settings;
using BambooCard.WebService.Abstractions;
using Bogus;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NSubstitute;
using WebServiceExchangeDto = BambooCard.WebService.Models.ExchangeRateDto;

namespace BambooCard.Tests.Business.Managers;

public class ExchangeManagerTests
{
    private readonly ICurrencyProviderFactory _prov;
    private readonly ICacheManager _cache;
    private readonly IExchangeRateRepository _repo;
    private readonly IOptions<CacheSettings> _cacheSettings;
    private readonly ExchangeManager _sut;
    private readonly BambooCardDbContext _db;

    public ExchangeManagerTests()
    {
        var options = new DbContextOptionsBuilder<BambooCardDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new BambooCardDbContext(options);
        _prov = Substitute.For<ICurrencyProviderFactory>();
        _cache = Substitute.For<ICacheManager>();

        var factory = new DummyProviderFactory();

        _repo = new ExchangeRateRepository(_db);
        _cacheSettings = Options.Create(new CacheSettings { Duration = 5 });

        _sut = new ExchangeManager(factory, _cache, _repo, _cacheSettings);

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
    public async Task SearchRatesAsync_ShouldFilterByDateRange()
    {
        var all = Seed(50, daysAgo: 50);

        var from = DateTime.UtcNow.AddDays(-25);
        var to = DateTime.UtcNow.AddDays(-10);

        var model = new ExchangeSearchModel
        {
            FromDate = from,
            ToDate = to,
            PageNumber = 1,
            PageSize = 100
        };

        var page = await _sut.SearchRatesAsync(model);

        Assert.All(page.Data, dto =>
        {
            Assert.True(dto.Date.Date >= from.Date && dto.Date.Date <= to.Date);
        });
    }

    [Theory]
    [InlineData(1, 10)]
    [InlineData(2, 10)]
    [InlineData(3, 7)]
    public async Task SearchRatesAsync_Pagination_Works(int pageNumber, int pageSize)
    {
        var dataCount = 25;
        var all = Seed(dataCount, daysAgo: 30);

        var model = new ExchangeSearchModel
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var page = await _sut.SearchRatesAsync(model);

        // total still 25
        Assert.Equal(dataCount, page.TotalRecords);

        // expected count = min(pageSize, remaining items)
        var expectedCount = Math.Min(pageSize, dataCount - (pageNumber - 1) * pageSize);
        Assert.Equal(expectedCount, page.Data.Count());

        // the first Date in the page should match the (offset)th item of all[]
        var expectedFirst = all[(pageNumber - 1) * pageSize];
        Assert.Equal(expectedFirst.Date, page.Data.First().Date);
    }

    [Fact]
    public async Task ImportLatestAsync_ShouldUpsertAndRefreshCache()
    {
        // ─── Arrange ───

        // 1) Fake the WebService DTO returned by your ICurrencyProvider
        var wsDto = new BambooCard.WebService.Models.ExchangeRateDto
        {
            Base = "EUR",
            Rates = new Dictionary<string, decimal> { ["USD"] = 1.1m }
        };
        var provider = Substitute.For<ICurrencyProvider>();
        provider
            .FetchLatestAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(wsDto));
        _prov
            .GetProvider(EProvider.Frankfurter)
            .Returns(provider);

        // 2) Stub out the IExchangeRateRepository so we never run EF‐Core logic here
        var repo = Substitute.For<IExchangeRateRepository>();
        repo.AddIfNotExists(Arg.Any<ExchangeRate>())
            .Returns(ci => Task.FromResult(ci.Arg<ExchangeRate>()));   // echo the passed entity

        repo.SaveChangesAsync().Returns(1);

        // 3) Build a fresh manager using the stubbed repo
        var manager = new ExchangeManager(_prov, _cache, repo, _cacheSettings);

        // ─── Act ───
        var result = await manager.ImportLatestAsync(EProvider.Frankfurter);

        // ─── Assert the returned DTO was properly adapted ───
        Assert.Equal(ECurrency.EUR, result.BaseCurrency);
        Assert.Equal(1.1m, result.Rates[ECurrency.USD]);

        // ─── Verify that repository methods were called ───
        await repo.Received(1).AddIfNotExists(Arg.Any<ExchangeRate>());
        await repo.Received(1).SaveChangesAsync();

        // ─── Verify that cache was refreshed in order ───
        var key = KeyHelper.GetCacheKey(EKeyType.ExchangeLatest);
        Received.InOrder(() =>
        {
            _cache.Remove(key);
            _cache.Add(key, result, _cacheSettings.Value.Duration);
        });
    }

    [Fact]
    public async Task GetServiceRateAsync_ShouldReturnAdaptedDto()
    {
        // Arrange 
        TypeAdapterConfig<BambooCard.WebService.Models.ExchangeRateDto, BambooCard.Business.Models.Main.ExchangeRateDto>
            .NewConfig()
            .Map(d => d.BaseCurrency, src => Enum.Parse<ECurrency>(src.Base, true))
            .Map(d => d.Rates,
                 src => src.Rates.ToDictionary(
                           kv => Enum.Parse<ECurrency>(kv.Key, true),
                           kv => kv.Value));

        var wsDto = new BambooCard.WebService.Models.ExchangeRateDto
        {
            Amount = 1,
            Date = DateTimeOffset.UtcNow,
            Base = "EUR",
            Rates = new Dictionary<string, decimal> { ["USD"] = 0.9m }
        };

        var provider = Substitute.For<ICurrencyProvider>();
        provider
            .FetchLatestAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(wsDto));

        var factory = Substitute.For<ICurrencyProviderFactory>();
        factory
            .GetProvider(EProvider.Frankfurter)
            .Returns(provider);

        var cache = Substitute.For<ICacheManager>();
        var options = new DbContextOptionsBuilder<BambooCardDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var realRepo = new ExchangeRateRepository(new BambooCardDbContext(options));
        var settings = Options.Create(new CacheSettings { Duration = 5 });

        var manager = new ExchangeManager(factory, cache, realRepo, settings);

        // ─── Act ───
        var result = await manager.GetServiceRateAsync(EProvider.Frankfurter);

        // ─── Assert ───
        Assert.NotNull(result);
        Assert.Equal(wsDto.Base, result.BaseCurrency.ToString());
        Assert.Equal(wsDto.Amount, result.Amount);
        Assert.Equal(wsDto.Date, result.Date);
        Assert.Single(result.Rates);
        Assert.Equal(0.9m, result.Rates[ECurrency.USD]);
    }

    [Fact]
    public async Task GetCurrentRateAsync_ShouldReturnCached_WhenCacheHit()
    {
        // Arrange
        var key = KeyHelper.GetCacheKey(EKeyType.ExchangeLatest);
        var cached = new BambooCard.Business.Models.Main.ExchangeRateDto
        {
            BaseCurrency = ECurrency.USD,
            Rates = new Dictionary<ECurrency, decimal>
            {
                [ECurrency.EUR] = 0.9m
            }
        };

        _cache.IsAdd(key).Returns(true);
        _cache.Get<ExchangeRateDto>(key).Returns(cached);

        // Act
        var result = await _sut.GetCurrentRateAsync();

        // Assert
        Assert.Same(cached, result);
        _cache.DidNotReceiveWithAnyArgs().Add(default, default, default);
    }


    [Fact]
    public async Task GetCurrentRateAsync_ShouldFetchDb_WhenCacheMiss()
    {
        var key = KeyHelper.GetCacheKey(EKeyType.ExchangeLatest);
        _cache.IsAdd(key).Returns(false);

        var entity = new ExchangeRate
        {
            BaseCurrency = ECurrency.GBP,
            Rates = new List<Rate>
        {
            new Rate { Currency = ECurrency.USD, Value = 1.3m }
        }
        };
        _db.ExchangeRates.Add(entity);
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.GetCurrentRateAsync();

        // Assert
        Assert.Equal(ECurrency.GBP, result.BaseCurrency);
        _cache.Received().Add(key, result, _cacheSettings.Value.Duration);
    }

    [Fact]
    public async Task GetCurrentRateAsync_ShouldThrow_NotFound_WhenDbEmpty()
    {
        _cache.IsAdd(Arg.Any<string>()).Returns(false);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.GetCurrentRateAsync()
        );
    }

    [Fact]
    public async Task RefreshCacheFromDbAsync_ShouldRemoveAndAddCache()
    {
        // Arrange
        var entity = new ExchangeRate
        {
            BaseCurrency = ECurrency.USD,
            Rates = new List<Rate>()
        };
        _db.ExchangeRates.Add(entity);
        await _db.SaveChangesAsync();

        var expectedDto = entity.Adapt<ExchangeRateDto>();
        var key = KeyHelper.GetCacheKey(EKeyType.ExchangeLatest);

        // Act
        await _sut.RefreshCacheFromDbAsync();

        // Assert
        _cache.Received(1).Remove(key);

        _cache.Received(1).Add(
            key,
            Arg.Is<ExchangeRateDto>(dto =>
                dto.BaseCurrency == expectedDto.BaseCurrency &&
                dto.Rates.Count == expectedDto.Rates.Count
            ),
            _cacheSettings.Value.Duration
        );
    }

    private List<ExchangeRate> Seed(int count = 25, int daysAgo = 30)
    {
        var faker = new Faker<ExchangeRate>()
            .RuleFor(x => x.BaseCurrency, f => f.PickRandom(ECurrency.USD, ECurrency.EUR, ECurrency.GBP))
            .RuleFor(x => x.Date, f => f.Date.Between(DateTime.UtcNow.AddDays(-daysAgo), DateTime.UtcNow))
            .RuleFor(x => x.Rates, f => new List<Rate> {
                new Rate{ Currency = ECurrency.USD, Value = f.Random.Decimal(.5m,1.5m) },
                new Rate{ Currency = ECurrency.EUR, Value = f.Random.Decimal(.5m,1.5m) },
                new Rate{ Currency = ECurrency.GBP, Value = f.Random.Decimal(.5m,1.5m) }
            });

        var list = faker.Generate(count)
                        .OrderBy(e => e.Date)
                        .ToList();

        _db.ExchangeRates.AddRange(list);
        _db.SaveChanges();
        return list;
    }

    private class DummyCacheManager : ICacheManager
    {
        public bool IsAdd(string key) => false;
        public void Add(string key, object value, int duration) { }
        public T Get<T>(string key) => default;
        public object Get(string key) => null;
        public void Remove(string key) { }
        public void RemoveByPattern(string pattern) { }
        public PaginationResult<T> GetPaginated<T>(string key, int pageIndex, int pageSize) => null;
    }

    private class DummyProviderFactory : ICurrencyProviderFactory
    {
        public ICurrencyProvider GetProvider(EProvider provider)
            => Substitute.For<ICurrencyProvider>();
    }
}

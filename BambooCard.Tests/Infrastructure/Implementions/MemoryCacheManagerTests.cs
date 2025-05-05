using BambooCard.Infrastructure.Implementions;
using Microsoft.Extensions.Caching.Memory;

namespace BambooCard.Tests.Infrastructure.Implementions;

public class MemoryCacheManagerTests
{
    private readonly IMemoryCache _memoryCache;
    private readonly MemoryCacheManager _cacheManager;

    public MemoryCacheManagerTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _cacheManager = new MemoryCacheManager(_memoryCache);
    }

    [Fact]
    public void Add_And_Get_ShouldWork()
    {
        _cacheManager.Add("key1", "value1", 10);
        var result = _cacheManager.Get<string>("key1");
        Assert.Equal("value1", result);
    }

    [Fact]
    public void Get_ShouldReturnNull_IfKeyMissing()
    {
        var result = _cacheManager.Get("unknownKey");
        Assert.Null(result);
    }

    [Fact]
    public void IsAdd_ShouldReturnTrue_IfKeyExists()
    {
        _cacheManager.Add("key2", 42, 5);
        Assert.True(_cacheManager.IsAdd("key2"));
    }

    [Fact]
    public void IsAdd_ShouldReturnFalse_IfKeyMissing()
    {
        Assert.False(_cacheManager.IsAdd("missingKey"));
    }

    [Fact]
    public void Remove_ShouldDeleteKey()
    {
        _cacheManager.Add("key3", "temp", 5);
        _cacheManager.Remove("key3");
        Assert.False(_cacheManager.IsAdd("key3"));
    }

    [Fact]
    public void GetPaginated_ShouldReturnCorrectPage()
    {
        var list = Enumerable.Range(1, 50).ToList();
        _cacheManager.Add("pagedList", list, 5);

        var page = _cacheManager.GetPaginated<int>("pagedList", 2, 10);

        Assert.Equal(10, page.Data.Count());
        Assert.Equal(50, page.TotalRecords);
        Assert.Equal(2, page.PageNumber);
        Assert.Equal(10, page.PageSize);
    }

    [Fact]
    public void GetPaginated_ShouldReturnNull_WhenPageTooHigh()
    {
        var list = Enumerable.Range(1, 20).ToList();
        _cacheManager.Add("shortList", list, 5);

        var page = _cacheManager.GetPaginated<int>("shortList", 5, 10);
        Assert.Null(page);
    }
}

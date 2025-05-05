using BambooCard.Infrastructure.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using System.Text.RegularExpressions;
using BambooCard.Infrastructure.Results;
using System.Reflection;

namespace BambooCard.Infrastructure.Implementions;

public class MemoryCacheManager(IMemoryCache cache) : ICacheManager
{
    public void Add(string key, object value, int duration)
        => cache.Set(key, value, TimeSpan.FromMinutes(duration));

    public T Get<T>(string key)
        => cache.Get<T>(key);

    public PaginationResult<T> GetPaginated<T>(string key, int pageIndex, int pageSize)
    {
        List<T> allResult = cache.Get<List<T>>(key);

        int totalItems = allResult.Count;

        if (pageIndex < 1)
            pageIndex = 1;

        if (pageIndex > (int)Math.Ceiling((double)totalItems / pageSize))
            return null;

        if (pageSize < 1)
            pageSize = 10;

        int startIndex = (pageIndex - 1) * pageSize;
        int endIndex = Math.Min(startIndex + pageSize, totalItems);

        IEnumerable<T> pageItems = allResult.GetRange(startIndex, endIndex - startIndex);

        return new PaginationResult<T>(pageItems, totalItems, pageIndex, pageSize);
    }

    public object Get(string key)
        => cache.Get(key);

    public bool IsAdd(string key)
        => cache.TryGetValue(key, out _);

    public void Remove(string key)
        => cache.Remove(key);

    public void RemoveByPattern(string pattern)
    {
        var entriesProperty = typeof(MemoryCache)
            .GetProperty("EntriesCollection", BindingFlags.NonPublic | BindingFlags.Instance);

        if (entriesProperty == null)
            throw new InvalidOperationException("Cannot access cache entries for pattern removal. Reflection target not found.");

        var entries = entriesProperty.GetValue(cache) as dynamic;

        var values = new List<ICacheEntry>();

        foreach (var item in entries)
        {
            var entry = item.GetType().GetProperty("Value").GetValue(item, null);
            values.Add(entry);
        }

        var regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        var keysToRemove = values.Where(d => regex.IsMatch(d.Key.ToString())).Select(d => d.Key.ToString()).ToList();

        foreach (var key in keysToRemove)
        {
            Remove(key);
        }

        //var cacheEntriesCollectionDefinition = typeof(MemoryCache).GetProperty("EntriesCollection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        //var cacheEntriesCollection = cacheEntriesCollectionDefinition.GetValue(cache) as dynamic;

        //List<ICacheEntry> cacheCollectionValues = new List<ICacheEntry>();

        //foreach (var cacheItem in cacheEntriesCollection)
        //{

        //    ICacheEntry cacheItemValue = cacheItem.GetType().GetProperty("Value").GetValue(cacheItem, null);

        //    cacheCollectionValues.Add(cacheItemValue);
        //}

        //var regex = new Regex(pattern, RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase);
        //var keysToRemove = cacheCollectionValues.Where(d => regex.IsMatch(d.Key.ToString())).Select(d => d.Key).ToList();

        //foreach (string key in keysToRemove)
        //{
        //    Remove(key);
        //}
    }
}
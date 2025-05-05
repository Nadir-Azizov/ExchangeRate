using BambooCard.Infrastructure.Results;

namespace BambooCard.Infrastructure.Abstractions;

public interface ICacheManager
{
    T Get<T>(string key);
    PaginationResult<T> GetPaginated<T>(string key, int pageIndex, int pageSize);
    object Get(string key);
    void Add(string key, object value, int duration);
    bool IsAdd(string key);
    void Remove(string key);
    void RemoveByPattern(string pattern);
}

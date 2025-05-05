using BambooCard.Infrastructure.Enums;

namespace BambooCard.Infrastructure.Helper;

public static class KeyHelper
{
    public static string GetJobKey(EKeyType jobType) => $"Job:{jobType}";
    public static string GetCacheKey(EKeyType cacheType) => $"Cache:{cacheType}";
}

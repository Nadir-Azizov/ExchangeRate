using BambooCard.Infrastructure.Enums;
using BambooCard.Infrastructure.Helper;

namespace BambooCard.Tests.Infrastructure.Helper;


public class KeyHelperTests
{
    [Theory]
    [InlineData(EKeyType.ExchangeLatest, "Job:ExchangeLatest")]
    public void GetJobKey_ShouldReturnCorrectFormat(EKeyType type, string expected)
    {
        var result = KeyHelper.GetJobKey(type);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(EKeyType.ExchangeLatest, "Cache:ExchangeLatest")]
    public void GetCacheKey_ShouldReturnCorrectFormat(EKeyType type, string expected)
    {
        var result = KeyHelper.GetCacheKey(type);
        Assert.Equal(expected, result);
    }
}

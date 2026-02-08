using Site;
using Xunit;

namespace SiteTests;

public class ApiKeysOptionsTest
{
    [Fact]
    public void Location_IsApiKeys()
    {
        Assert.Equal("ApiKeys", ApiKeysOptions.Location);
    }

    [Fact]
    public void ValidKeys_DefaultsToEmptyList()
    {
        var options = new ApiKeysOptions();
        Assert.NotNull(options.ValidKeys);
        Assert.Empty(options.ValidKeys);
    }

    [Fact]
    public void ValidKeys_CanBeSet()
    {
        var options = new ApiKeysOptions
        {
            ValidKeys = new List<string> { "key1", "key2" }
        };

        Assert.Equal(2, options.ValidKeys.Count);
        Assert.Contains("key1", options.ValidKeys);
    }
}

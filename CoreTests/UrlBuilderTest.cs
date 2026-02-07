using Core.Helpers;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CoreTests;

public class UrlBuilderTest
{
    private static IConfiguration BuildConfig(string? baseUrl)
    {
        var configData = new Dictionary<string, string?>();
        if (baseUrl != null)
            configData["BaseUrl"] = baseUrl;

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
    }

    [Fact]
    public void BuildUrl_NoRestPath_ReturnsBaseUrl()
    {
        var builder = new UrlBuilder(BuildConfig("https://wateralarm.be"));

        var url = builder.BuildUrl();

        Assert.Equal("https://wateralarm.be", url);
    }

    [Fact]
    public void BuildUrl_WithRestPath_AppendsPath()
    {
        var builder = new UrlBuilder(BuildConfig("https://wateralarm.be"));

        var url = builder.BuildUrl("/a/mylink/s/mysensor");

        Assert.Equal("https://wateralarm.be/a/mylink/s/mysensor", url);
    }

    [Fact]
    public void BuildUrl_BaseUrlWithTrailingSlash_TrimmedBeforeAppend()
    {
        var builder = new UrlBuilder(BuildConfig("https://wateralarm.be/"));

        var url = builder.BuildUrl("/a/test");

        Assert.Equal("https://wateralarm.be/a/test", url);
    }

    [Fact]
    public void BuildUrl_NullBaseUrl_UsesDefault()
    {
        var builder = new UrlBuilder(BuildConfig(null));

        var url = builder.BuildUrl("/a/test");

        Assert.Equal("https://wateralarm.be/a/test", url);
    }

    [Fact]
    public void BuildUrl_NullRestPath_ReturnsBaseUrl()
    {
        var builder = new UrlBuilder(BuildConfig("https://wateralarm.be"));

        var url = builder.BuildUrl(null);

        Assert.Equal("https://wateralarm.be", url);
    }

    [Fact]
    public void BuildUrl_EmptyRestPath_ReturnsBaseUrl()
    {
        var builder = new UrlBuilder(BuildConfig("https://wateralarm.be"));

        var url = builder.BuildUrl("");

        Assert.Equal("https://wateralarm.be", url);
    }

    [Fact]
    public void BuildUrl_NullBaseUrl_NoRestPath_ReturnsDefault()
    {
        var builder = new UrlBuilder(BuildConfig(null));

        var url = builder.BuildUrl();

        Assert.Equal("https://wateralarm.be", url);
    }
}

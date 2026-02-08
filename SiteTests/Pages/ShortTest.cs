using Microsoft.AspNetCore.Mvc;
using Site.Pages;
using SiteTests.Helpers;

namespace SiteTests.Pages;

public class ShortTest
{
    [Fact]
    public void OnGet_RedirectsToBlog_WhenNoContext()
    {
        var model = new Short();
        TestEntityFactory.SetupPageContext(model);

        var result = model.OnGet(null);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("https://blog.wateralarm.be", redirect.Url);
    }

    [Fact]
    public void OnGet_RedirectsToBlog_WhenContextHasFewerThan3Parts()
    {
        var model = new Short();
        TestEntityFactory.SetupPageContext(model);

        var result = model.OnGet("source|medium");

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("https://blog.wateralarm.be", redirect.Url);
    }

    [Fact]
    public void OnGet_RedirectsWithUtmParams_When3Parts()
    {
        var model = new Short();
        TestEntityFactory.SetupPageContext(model);

        var result = model.OnGet("qr|scan|campaign1");

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.StartsWith("https://blog.wateralarm.be?", redirect.Url);
        Assert.Contains("utm_source=qr", redirect.Url);
        Assert.Contains("utm_medium=scan", redirect.Url);
        Assert.Contains("utm_campaign=campaign1", redirect.Url);
        Assert.DoesNotContain("utm_id", redirect.Url);
    }

    [Fact]
    public void OnGet_RedirectsWithUtmIdParam_When4Parts()
    {
        var model = new Short();
        TestEntityFactory.SetupPageContext(model);

        var result = model.OnGet("qr|scan|campaign1|myid");

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Contains("utm_source=qr", redirect.Url);
        Assert.Contains("utm_medium=scan", redirect.Url);
        Assert.Contains("utm_campaign=campaign1", redirect.Url);
        Assert.Contains("utm_id=myid", redirect.Url);
    }

    [Fact]
    public void OnGet_RedirectsToBlog_WhenContextIs1Part()
    {
        var model = new Short();
        TestEntityFactory.SetupPageContext(model);

        var result = model.OnGet("onlyonepart");

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("https://blog.wateralarm.be", redirect.Url);
    }

    [Fact]
    public void OnGet_HandlesEmptyStringContext()
    {
        var model = new Short();
        TestEntityFactory.SetupPageContext(model);

        var result = model.OnGet("");

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("https://blog.wateralarm.be", redirect.Url);
    }
}

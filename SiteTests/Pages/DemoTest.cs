using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Site.Pages;
using SiteTests.Helpers;

namespace SiteTests.Pages;

public class DemoTest
{
    [Fact]
    public void OnGet_RedirectsToConfiguredPath()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DemoPath"] = "/a/custom-demo"
            })
            .Build();

        var model = new Demo(config);
        TestEntityFactory.SetupPageContext(model);

        var result = model.OnGet();

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/a/custom-demo", redirect.Url);
    }

    [Fact]
    public void OnGet_RedirectsToDefault_WhenConfigNotSet()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var model = new Demo(config);
        TestEntityFactory.SetupPageContext(model);

        var result = model.OnGet();

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/a/demo/s/f2y616afaEA", redirect.Url);
    }
}

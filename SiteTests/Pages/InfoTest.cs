using Microsoft.AspNetCore.Http;
using Site.Pages;
using SiteTests.Helpers;

namespace SiteTests.Pages;

public class InfoTest
{
    [Fact]
    public void OnGet_SetsServerToMachineName()
    {
        var model = new Info();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString("localhost", 5000);
        httpContext.Request.Headers.UserAgent = "TestAgent/1.0";
        TestEntityFactory.SetupPageContext(model, httpContext);

        model.OnGet();

        Assert.Equal(Environment.MachineName, model.Server);
    }

    [Fact]
    public void OnGet_SetsRequestHost()
    {
        var model = new Info();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString("myhost.example.com");
        TestEntityFactory.SetupPageContext(model, httpContext);

        model.OnGet();

        Assert.Equal("myhost.example.com", model.RequestHost);
    }

    [Fact]
    public void OnGet_SetsUserAgent()
    {
        var model = new Info();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.UserAgent = "CustomAgent/2.0";
        TestEntityFactory.SetupPageContext(model, httpContext);

        model.OnGet();

        Assert.Equal("CustomAgent/2.0", model.UserAgent);
    }

    [Fact]
    public void OnGet_SetsOsVersion()
    {
        var model = new Info();
        TestEntityFactory.SetupPageContext(model);

        model.OnGet();

        Assert.NotNull(model.OsVersion);
        Assert.NotEmpty(model.OsVersion);
    }

    [Fact]
    public void OnGet_SetsDotNetVersion()
    {
        var model = new Info();
        TestEntityFactory.SetupPageContext(model);

        model.OnGet();

        Assert.NotNull(model.DotNetVersion);
        Assert.NotEmpty(model.DotNetVersion);
    }

    [Fact]
    public void OnGet_SetsVersion()
    {
        var model = new Info();
        TestEntityFactory.SetupPageContext(model);

        model.OnGet();

        Assert.NotNull(model.Version);
    }

    [Fact]
    public void OnGet_UserAuthId_IsNullForAnonymous()
    {
        var model = new Info();
        TestEntityFactory.SetupPageContext(model);

        model.OnGet();

        Assert.Null(model.UserAuthId);
    }
}

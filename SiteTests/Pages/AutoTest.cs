using Core.Queries;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Site.Pages;
using SiteTests.Helpers;

namespace SiteTests.Pages;

public class AutoTest
{
    private static Auto CreateModel(ConfigurableFakeMediator? mediator = null, string? cookieValue = null, FakeUserInfo? userInfo = null)
    {
        mediator ??= new ConfigurableFakeMediator();
        userInfo ??= new FakeUserInfo();
        var model = new Auto(mediator, userInfo);
        var httpContext = new DefaultHttpContext();
        if (cookieValue != null)
        {
            httpContext.Request.Headers.Append("Cookie", $"auto={cookieValue}");
        }
        TestEntityFactory.SetupPageContext(model, httpContext);
        return model;
    }

    [Fact]
    public async Task OnGet_ShowsPage_WhenNoCookie()
    {
        var model = CreateModel();

        var result = await model.OnGet();

        Assert.IsType<Microsoft.AspNetCore.Mvc.RazorPages.PageResult>(result);
        Assert.Null(model.Link);
    }

    [Fact]
    public async Task OnGet_ShowsPage_WhenUpdateIsTrue()
    {
        var mediator = new ConfigurableFakeMediator();
        var model = CreateModel(mediator, "/a/test-link");

        var result = await model.OnGet(update: true);

        Assert.IsType<Microsoft.AspNetCore.Mvc.RazorPages.PageResult>(result);
        Assert.Equal("/a/test-link", model.Link);
    }

    [Fact]
    public async Task OnGet_Redirects_WhenCookieHasValidAccountSensorLink()
    {
        var mediator = new ConfigurableFakeMediator();
        var accountSensor = TestEntityFactory.CreateAccountSensor();
        mediator.SetResponse<AccountSensorByLinkQuery, Core.Entities.AccountSensor?>(accountSensor);

        var model = CreateModel(mediator, "/a/test-link/s/test-sensor-link");

        var result = await model.OnGet();

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/a/test-link/s/test-sensor-link", redirect.Url);
    }

    [Fact]
    public async Task OnGet_Redirects_WhenCookieHasValidAccountLink()
    {
        var mediator = new ConfigurableFakeMediator();
        var account = TestEntityFactory.CreateAccount();
        mediator.SetResponse<AccountByLinkQuery, Core.Entities.Account?>(account);

        var model = CreateModel(mediator, "/a/test-link");

        var result = await model.OnGet();

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/a/test-link", redirect.Url);
    }

    [Fact]
    public async Task OnGet_ShowsPage_WhenCookieLinkNotFound()
    {
        var mediator = new ConfigurableFakeMediator();
        // No responses registered → returns null

        var model = CreateModel(mediator, "/a/nonexistent/s/missing");

        var result = await model.OnGet();

        Assert.IsType<Microsoft.AspNetCore.Mvc.RazorPages.PageResult>(result);
    }

    [Fact]
    public async Task OnGet_ShowsPage_WhenCookieHasInvalidFormat()
    {
        var mediator = new ConfigurableFakeMediator();
        var model = CreateModel(mediator, "invalid-url");

        var result = await model.OnGet();

        // Invalid URL doesn't match the regex, TestAutoLink returns null
        Assert.IsType<Microsoft.AspNetCore.Mvc.RazorPages.PageResult>(result);
    }

    [Fact]
    public void OnPost_SetsCookie_WhenLinkProvided()
    {
        var model = CreateModel();

        var result = model.OnPost("/a/my-link");

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/auto", redirect.Url);
        Assert.True(model.HttpContext.Response.Headers.ContainsKey("Set-Cookie"));
    }

    [Fact]
    public void OnPost_DeletesCookie_WhenLinkIsEmpty()
    {
        var model = CreateModel(cookieValue: "/a/old");

        var result = model.OnPost("");

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/auto", redirect.Url);
    }

    [Fact]
    public void OnPost_DeletesCookie_WhenLinkIsNull()
    {
        var model = CreateModel(cookieValue: "/a/old");

        var result = model.OnPost(null!);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/auto", redirect.Url);
    }

    [Fact]
    public async Task OnGet_UpdatesCookie_WhenCanonicalUrlDiffersFromCookie()
    {
        var mediator = new ConfigurableFakeMediator();
        var accountSensor = TestEntityFactory.CreateAccountSensor();
        mediator.SetResponse<AccountSensorByLinkQuery, Core.Entities.AccountSensor?>(accountSensor);

        // Cookie has trailing extra path that will be stripped by TestAutoLink
        var model = CreateModel(mediator, "/a/test-link/s/test-sensor-link/extra-stuff");

        var result = await model.OnGet();

        var redirect = Assert.IsType<RedirectResult>(result);
        // TestAutoLink canonicalizes to /a/test-link/s/test-sensor-link
        Assert.Equal("/a/test-link/s/test-sensor-link", redirect.Url);
        // Cookie should be updated (Set-Cookie header present)
        Assert.True(model.HttpContext.Response.Headers.ContainsKey("Set-Cookie"));
    }

    [Fact]
    public async Task OnGet_RedirectsToAdminDashboard_WhenAuthenticatedAdminWithoutAccountContext()
    {
        var mediator = new ConfigurableFakeMediator();
        var userInfo = new FakeUserInfo { Authenticated = true, Admin = true, LoginEmail = "admin@test.com" };
        var model = CreateModel(mediator, userInfo: userInfo);

        model.HttpContext.User = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(
                [new System.Security.Claims.Claim("email", "admin@test.com")],
                "test-auth"));

        var result = await model.OnGet();

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/adm", redirect.Url);
    }

    [Fact]
    public async Task OnGet_ShowsPage_WhenAuthenticatedNonAdminWithoutAccountContext()
    {
        var mediator = new ConfigurableFakeMediator();
        var userInfo = new FakeUserInfo { Authenticated = true, Admin = false, LoginEmail = "user@test.com" };
        var model = CreateModel(mediator, userInfo: userInfo);

        model.HttpContext.User = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(
                [new System.Security.Claims.Claim("email", "user@test.com")],
                "test-auth"));

        var result = await model.OnGet();

        Assert.IsType<Microsoft.AspNetCore.Mvc.RazorPages.PageResult>(result);
    }
}

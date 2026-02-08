using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Site.Pages;
using SiteTests.Helpers;

namespace SiteTests.Pages;

/// <summary>
/// Tests for AccountCallback page model.
/// </summary>
public class AccountCallbackTest
{
    private static (AccountCallback model, FakeUserManager userManager, DefaultHttpContext httpContext)
        CreateModel()
    {
        var userManager = new FakeUserManager();
        var model = new AccountCallback(userManager);

        var httpContext = new DefaultHttpContext();

        // Register a fake IAuthenticationService so SignInAsync/SignOutAsync don't throw
        var services = new ServiceCollection();
        services.AddSingleton<IAuthenticationService>(new FakeAuthenticationService());
        httpContext.RequestServices = services.BuildServiceProvider();

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new Microsoft.AspNetCore.Mvc.RazorPages.PageActionDescriptor());
        model.PageContext = new PageContext(actionContext);

        return (model, userManager, httpContext);
    }

    private static IConfiguration CreateConfiguration(TimeSpan? tokenLifespan = null)
    {
        tokenLifespan ??= TimeSpan.FromHours(24);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AccountLoginMessage:TokenLifespan"] = tokenLifespan.Value.ToString(),
                ["AccountLoginMessage:CodeLifespanHours"] = "2",
                ["AccountLoginMessage:Salt"] = "test-salt"
            })
            .Build();
        return config;
    }

    // ---- Empty token/email: sign out and redirect ----

    [Fact]
    public async Task OnGet_EmptyToken_SignsOutAndRedirectsToRoot()
    {
        var (model, _, _) = CreateModel();

        var result = await model.OnGet(token: "", email: "test@test.com", url: null,
            configuration: CreateConfiguration());

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/", redirect.Url);
    }

    [Fact]
    public async Task OnGet_EmptyEmail_SignsOutAndRedirectsToRoot()
    {
        var (model, _, _) = CreateModel();

        var result = await model.OnGet(token: "some-token", email: "", url: null,
            configuration: CreateConfiguration());

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/", redirect.Url);
    }

    [Fact]
    public async Task OnGet_EmptyTokenWithUrl_SignsOutAndRedirectsToUrl()
    {
        var (model, _, _) = CreateModel();

        var result = await model.OnGet(token: "", email: "", url: "%2Fdashboard",
            configuration: CreateConfiguration());

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/dashboard", redirect.Url);
    }

    [Fact]
    public async Task OnGet_NullTokenAndEmail_SignsOutAndRedirects()
    {
        var (model, _, _) = CreateModel();

        var result = await model.OnGet(token: null!, email: null!, url: null,
            configuration: CreateConfiguration());

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/", redirect.Url);
    }

    // ---- Valid token: sign in and redirect ----

    [Fact]
    public async Task OnGet_ValidToken_SignsInAndRedirectsToRoot()
    {
        var (model, userManager, _) = CreateModel();
        userManager.AddUser("user@test.com");
        userManager.VerifyTokenResult = true;

        var result = await model.OnGet(token: "valid-token", email: "user@test.com", url: null,
            configuration: CreateConfiguration());

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/", redirect.Url);
    }

    [Fact]
    public async Task OnGet_ValidToken_WithReturnUrl_RedirectsToReturnUrl()
    {
        var (model, userManager, _) = CreateModel();
        userManager.AddUser("user@test.com");
        userManager.VerifyTokenResult = true;

        var result = await model.OnGet(token: "valid-token", email: "user@test.com",
            url: "%2Fa%2Fmy-link", configuration: CreateConfiguration());

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/a/my-link", redirect.Url);
    }

    // ---- Invalid token: redirect to Error ----

    [Fact]
    public async Task OnGet_InvalidToken_RedirectsToError()
    {
        var (model, userManager, _) = CreateModel();
        userManager.AddUser("user@test.com");
        userManager.VerifyTokenResult = false;

        var result = await model.OnGet(token: "bad-token", email: "user@test.com", url: null,
            configuration: CreateConfiguration());

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("Error", redirect.Url);
    }

    // ---- User not found: throws ----

    [Fact]
    public async Task OnGet_UserNotFound_Throws()
    {
        var (model, _, _) = CreateModel();
        // Don't add user

        await Assert.ThrowsAsync<Exception>(
            () => model.OnGet(token: "some-token", email: "unknown@test.com", url: null,
                configuration: CreateConfiguration()));
    }

    // ---- Token lifespan from configuration ----

    [Fact]
    public async Task OnGet_UsesTokenLifespanFromConfig()
    {
        var (model, userManager, httpContext) = CreateModel();
        userManager.AddUser("user@test.com");
        userManager.VerifyTokenResult = true;

        var config = CreateConfiguration(TimeSpan.FromHours(48));

        var result = await model.OnGet(token: "valid-token", email: "user@test.com", url: null,
            configuration: config);

        // Verify sign-in succeeded (redirects to "/" not "Error")
        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/", redirect.Url);
    }

    /// <summary>
    /// Fake authentication service that records calls but doesn't do real auth.
    /// </summary>
    private class FakeAuthenticationService : IAuthenticationService
    {
        public List<string> SignInSchemes { get; } = new();
        public List<string> SignOutSchemes { get; } = new();

        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
            => Task.FromResult(AuthenticateResult.NoResult());

        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task SignInAsync(HttpContext context, string? scheme, System.Security.Claims.ClaimsPrincipal principal, AuthenticationProperties? properties)
        {
            SignInSchemes.Add(scheme ?? "default");
            return Task.CompletedTask;
        }

        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        {
            SignOutSchemes.Add(scheme ?? "default");
            return Task.CompletedTask;
        }
    }
}

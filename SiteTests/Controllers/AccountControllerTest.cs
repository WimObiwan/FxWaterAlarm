using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Site.Controllers;
using SiteTests.Helpers;

namespace SiteTests.Controllers;

public class AccountControllerTest
{
    private class FakeAuthenticationService : IAuthenticationService
    {
        public bool SignedIn { get; private set; }
        public string? Scheme { get; private set; }

        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
            => Task.FromResult(AuthenticateResult.NoResult());

        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task SignInAsync(HttpContext context, string? scheme,
            System.Security.Claims.ClaimsPrincipal principal, AuthenticationProperties? properties)
        {
            SignedIn = true;
            Scheme = scheme;
            return Task.CompletedTask;
        }

        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;
    }

    private class FakeTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object?> LoadTempData(HttpContext context) => new Dictionary<string, object?>();
        public void SaveTempData(HttpContext context, IDictionary<string, object?> values) { }
    }

    private static (AccountController controller, FakeUserManager userManager, FakeAuthenticationService authService) CreateController()
    {
        var userManager = new FakeUserManager();
        var authService = new FakeAuthenticationService();
        var controller = new AccountController(userManager);

        var services = new ServiceCollection();
        services.AddSingleton<IAuthenticationService>(authService);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        controller.TempData = new TempDataDictionary(httpContext, new FakeTempDataProvider());

        return (controller, userManager, authService);
    }

    private static IConfiguration CreateConfiguration(string tokenLifespan = "01:00:00")
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AccountLoginMessage:TokenLifespan"] = tokenLifespan,
                ["AccountLoginMessage:CodeLifespanHours"] = "24",
                ["AccountLoginMessage:Salt"] = "test-salt"
            })
            .Build();
    }

    [Fact]
    public async Task LoginCallback_ValidToken_RedirectsToRoot()
    {
        var (controller, userManager, authService) = CreateController();
        userManager.AddUser("user@test.com");
        var config = CreateConfiguration();

        var result = await controller.LoginCallback("fake-token", "user@test.com", config);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/", redirect.Url);
        Assert.True(authService.SignedIn);
    }

    [Fact]
    public async Task LoginCallback_InvalidToken_ReturnsErrorView()
    {
        var (controller, userManager, _) = CreateController();
        userManager.AddUser("user@test.com");
        userManager.VerifyTokenResult = false;
        var config = CreateConfiguration();

        var result = await controller.LoginCallback("bad-token", "user@test.com", config);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Error", view.ViewName);
    }

    [Fact]
    public async Task LoginCallback_UserNotFound_ThrowsException()
    {
        var (controller, _, _) = CreateController();
        var config = CreateConfiguration();

        await Assert.ThrowsAsync<Exception>(() =>
            controller.LoginCallback("token", "nonexistent@test.com", config));
    }

    [Fact]
    public async Task LoginCallback_MissingConfig_ThrowsException()
    {
        var (controller, userManager, _) = CreateController();
        userManager.AddUser("user@test.com");
        // Empty configuration - no AccountLoginMessage section
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        await Assert.ThrowsAsync<Exception>(() =>
            controller.LoginCallback("fake-token", "user@test.com", config));
    }

    [Fact]
    public async Task LoginCallback_SignsInWithCorrectScheme()
    {
        var (controller, userManager, authService) = CreateController();
        userManager.AddUser("user@test.com");
        var config = CreateConfiguration();

        await controller.LoginCallback("fake-token", "user@test.com", config);

        Assert.Equal(IdentityConstants.ApplicationScheme, authService.Scheme);
    }
}

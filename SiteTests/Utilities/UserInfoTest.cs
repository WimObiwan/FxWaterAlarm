using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Site.Pages;
using Site.Utilities;
using Xunit;

namespace SiteTests.Utilities;

public class UserInfoTest
{
    /// <summary>
    /// Fake IAuthorizationService that returns a configurable result.
    /// </summary>
    private class FakeAuthorizationService : IAuthorizationService
    {
        public bool ShouldSucceed { get; set; }

        public Task<AuthorizationResult> AuthorizeAsync(
            ClaimsPrincipal user, object? resource, IEnumerable<IAuthorizationRequirement> requirements)
        {
            return Task.FromResult(ShouldSucceed
                ? AuthorizationResult.Success()
                : AuthorizationResult.Failed(AuthorizationFailure.ExplicitFail()));
        }

        public Task<AuthorizationResult> AuthorizeAsync(
            ClaimsPrincipal user, object? resource, string policyName)
        {
            return Task.FromResult(ShouldSucceed
                ? AuthorizationResult.Success()
                : AuthorizationResult.Failed(AuthorizationFailure.ExplicitFail()));
        }
    }

    private static UserInfo CreateUserInfo(
        HttpContext? httpContext = null,
        bool authorizationSucceeds = false)
    {
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var authService = new FakeAuthorizationService { ShouldSucceed = authorizationSucceeds };
        var options = Options.Create(new AccountLoginMessageOptions
        {
            TokenLifespanRaw = TimeSpan.FromHours(1),
            CodeLifespanHoursRaw = 24,
            SaltRaw = "test-salt"
        });
        return new UserInfo(httpContextAccessor, authService, options);
    }

    private static DefaultHttpContext CreateHttpContext(string? email = null, bool authenticated = true)
    {
        var context = new DefaultHttpContext();
        var claims = new List<Claim>();
        if (email != null)
            claims.Add(new Claim("email", email));
        var identity = authenticated
            ? new ClaimsIdentity(claims, "test")
            : new ClaimsIdentity(claims);
        context.User = new ClaimsPrincipal(identity);
        return context;
    }

    private static Core.Entities.Account CreateAccount(string email = "test@example.com")
    {
        return new Core.Entities.Account
        {
            Uid = Guid.NewGuid(),
            Email = email,
            CreationTimestamp = DateTime.UtcNow
        };
    }

    private static Core.Entities.AccountSensor CreateAccountSensor(string accountEmail = "test@example.com")
    {
        return new Core.Entities.AccountSensor
        {
            Account = CreateAccount(accountEmail),
            Sensor = new Core.Entities.Sensor
            {
                Uid = Guid.NewGuid(),
                DevEui = "test-sensor",
                CreateTimestamp = DateTime.UtcNow,
                Type = Core.Entities.SensorType.Level
            },
            CreateTimestamp = DateTime.UtcNow
        };
    }

    // --- IsAuthenticated ---

    [Fact]
    public void IsAuthenticated_ReturnsTrue_WhenAuthenticated()
    {
        var userInfo = CreateUserInfo(CreateHttpContext(authenticated: true));
        Assert.True(userInfo.IsAuthenticated());
    }

    [Fact]
    public void IsAuthenticated_ReturnsFalse_WhenNotAuthenticated()
    {
        var userInfo = CreateUserInfo(CreateHttpContext(authenticated: false));
        Assert.False(userInfo.IsAuthenticated());
    }

    [Fact]
    public void IsAuthenticated_ReturnsFalse_WhenNoHttpContext()
    {
        var userInfo = CreateUserInfo(httpContext: null);
        Assert.False(userInfo.IsAuthenticated());
    }

    // --- GetLoginEmail ---

    [Fact]
    public void GetLoginEmail_ReturnsEmail_WhenClaimExists()
    {
        var userInfo = CreateUserInfo(CreateHttpContext(email: "user@example.com"));
        Assert.Equal("user@example.com", userInfo.GetLoginEmail());
    }

    [Fact]
    public void GetLoginEmail_ReturnsNull_WhenNoEmailClaim()
    {
        var userInfo = CreateUserInfo(CreateHttpContext());
        Assert.Null(userInfo.GetLoginEmail());
    }

    [Fact]
    public void GetLoginEmail_ReturnsNull_WhenNoHttpContext()
    {
        var userInfo = CreateUserInfo(httpContext: null);
        Assert.Null(userInfo.GetLoginEmail());
    }

    // --- IsAdmin ---

    [Fact]
    public async Task IsAdmin_ReturnsTrue_WhenAuthorizationSucceeds()
    {
        var httpContext = CreateHttpContext(email: "admin@example.com");
        var userInfo = CreateUserInfo(httpContext, authorizationSucceeds: true);
        Assert.True(await userInfo.IsAdmin());
    }

    [Fact]
    public async Task IsAdmin_ReturnsFalse_WhenAuthorizationFails()
    {
        var httpContext = CreateHttpContext(email: "user@example.com");
        var userInfo = CreateUserInfo(httpContext, authorizationSucceeds: false);
        Assert.False(await userInfo.IsAdmin());
    }

    [Fact]
    public async Task IsAdmin_ReturnsFalse_WhenNoHttpContext()
    {
        var userInfo = CreateUserInfo(httpContext: null);
        Assert.False(await userInfo.IsAdmin());
    }

    [Fact]
    public async Task IsAdmin_ReturnsFalse_WhenNoUser()
    {
        // DefaultHttpContext with default (empty, non-null) User
        var httpContext = new DefaultHttpContext();
        httpContext.User = null!;
        var userInfo = CreateUserInfo(httpContext, authorizationSucceeds: false);
        // httpContext.User is null, so the pattern match `httpContext.User is { } user` fails
        Assert.False(await userInfo.IsAdmin());
    }

    // --- CanUpdateAccount ---

    [Fact]
    public async Task CanUpdateAccount_ReturnsTrue_WhenAdmin()
    {
        var httpContext = CreateHttpContext(email: "admin@example.com");
        var userInfo = CreateUserInfo(httpContext, authorizationSucceeds: true);
        var account = CreateAccount("other@example.com");
        Assert.True(await userInfo.CanUpdateAccount(account));
    }

    [Fact]
    public async Task CanUpdateAccount_ReturnsTrue_WhenEmailMatchesAccount()
    {
        var httpContext = CreateHttpContext(email: "user@example.com");
        var userInfo = CreateUserInfo(httpContext, authorizationSucceeds: false);
        var account = CreateAccount("user@example.com");
        Assert.True(await userInfo.CanUpdateAccount(account));
    }

    [Fact]
    public async Task CanUpdateAccount_ReturnsTrue_CaseInsensitiveEmailMatch()
    {
        var httpContext = CreateHttpContext(email: "USER@EXAMPLE.COM");
        var userInfo = CreateUserInfo(httpContext, authorizationSucceeds: false);
        var account = CreateAccount("user@example.com");
        Assert.True(await userInfo.CanUpdateAccount(account));
    }

    [Fact]
    public async Task CanUpdateAccount_ReturnsFalse_WhenEmailDoesNotMatch()
    {
        var httpContext = CreateHttpContext(email: "user@example.com");
        var userInfo = CreateUserInfo(httpContext, authorizationSucceeds: false);
        var account = CreateAccount("other@example.com");
        Assert.False(await userInfo.CanUpdateAccount(account));
    }

    [Fact]
    public async Task CanUpdateAccount_ReturnsFalse_WhenNoEmailClaim()
    {
        var httpContext = CreateHttpContext();
        var userInfo = CreateUserInfo(httpContext, authorizationSucceeds: false);
        var account = CreateAccount("test@example.com");
        Assert.False(await userInfo.CanUpdateAccount(account));
    }

    [Fact]
    public async Task CanUpdateAccount_ReturnsFalse_WhenNoHttpContext()
    {
        var userInfo = CreateUserInfo(httpContext: null, authorizationSucceeds: false);
        var account = CreateAccount("test@example.com");
        Assert.False(await userInfo.CanUpdateAccount(account));
    }

    // --- CanUpdateAccountSensor ---

    [Fact]
    public async Task CanUpdateAccountSensor_ReturnsTrue_WhenCanUpdateAccount()
    {
        var httpContext = CreateHttpContext(email: "user@example.com");
        var userInfo = CreateUserInfo(httpContext, authorizationSucceeds: false);
        var accountSensor = CreateAccountSensor("user@example.com");
        Assert.True(await userInfo.CanUpdateAccountSensor(accountSensor));
    }

    [Fact]
    public async Task CanUpdateAccountSensor_ReturnsFalse_WhenCannotUpdateAccount()
    {
        var httpContext = CreateHttpContext(email: "user@example.com");
        var userInfo = CreateUserInfo(httpContext, authorizationSucceeds: false);
        var accountSensor = CreateAccountSensor("other@example.com");
        Assert.False(await userInfo.CanUpdateAccountSensor(accountSensor));
    }
}

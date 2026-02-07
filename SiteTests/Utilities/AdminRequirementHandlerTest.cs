using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Site.Pages;
using Xunit;

namespace SiteTests.Utilities;

public class AdminRequirementHandlerTest
{
    private static AdminRequirementHandler CreateHandler(
        AccountLoginMessageOptions options,
        HttpContext? httpContext = null)
    {
        var optionsWrapper = Options.Create(options);
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var logger = NullLogger<AdminRequirementHandler>.Instance;
        return new AdminRequirementHandler(optionsWrapper, httpContextAccessor, logger);
    }

    private static AuthorizationHandlerContext CreateContext(ClaimsPrincipal user)
    {
        var requirement = new AdminRequirement();
        return new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null);
    }

    private static ClaimsPrincipal CreateUser(string? email = null)
    {
        var claims = new List<Claim>();
        if (email != null)
            claims.Add(new Claim("email", email));
        var identity = new ClaimsIdentity(claims, "test");
        return new ClaimsPrincipal(identity);
    }

    private static DefaultHttpContext CreateHttpContext(string ipAddress = "127.0.0.1")
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(ipAddress);
        return context;
    }

    private static AccountLoginMessageOptions CreateOptions(
        string[]? adminEmails = null,
        string[]? adminIPs = null)
    {
        return new AccountLoginMessageOptions
        {
            TokenLifespanRaw = TimeSpan.FromHours(1),
            CodeLifespanHoursRaw = 24,
            SaltRaw = "test-salt",
            AdminEmails = adminEmails,
            AdminIPsRaw = adminIPs
        };
    }

    private static async Task<AuthorizationHandlerContext> RunHandler(
        AccountLoginMessageOptions options,
        ClaimsPrincipal user,
        HttpContext? httpContext)
    {
        var handler = CreateHandler(options, httpContext);
        var context = CreateContext(user);
        await ((IAuthorizationHandler)handler).HandleAsync(context);
        return context;
    }

    [Fact]
    public async Task Succeeds_WhenEmailMatchesAndNoIpRestriction()
    {
        var options = CreateOptions(adminEmails: ["admin@example.com"]);
        var httpContext = CreateHttpContext("192.168.1.1");

        var context = await RunHandler(options, CreateUser("admin@example.com"), httpContext);

        Assert.True(context.HasSucceeded);
        Assert.False(context.HasFailed);
    }

    [Fact]
    public async Task Succeeds_WhenEmailMatchesAndIpInRange()
    {
        var options = CreateOptions(
            adminEmails: ["admin@example.com"],
            adminIPs: ["192.168.1.0/24"]);
        var httpContext = CreateHttpContext("192.168.1.100");

        var context = await RunHandler(options, CreateUser("admin@example.com"), httpContext);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Succeeds_EmailMatchingIsCaseInsensitive()
    {
        var options = CreateOptions(adminEmails: ["admin@example.com"]);
        var httpContext = CreateHttpContext();

        var context = await RunHandler(options, CreateUser("ADMIN@EXAMPLE.COM"), httpContext);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Fails_WhenNoEmailClaim()
    {
        var options = CreateOptions(adminEmails: ["admin@example.com"]);
        var httpContext = CreateHttpContext();

        var context = await RunHandler(options, CreateUser(), httpContext);

        Assert.True(context.HasFailed);
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Fails_WhenAdminEmailsIsNull()
    {
        var options = CreateOptions(adminEmails: null);
        var httpContext = CreateHttpContext();

        var context = await RunHandler(options, CreateUser("admin@example.com"), httpContext);

        Assert.True(context.HasFailed);
    }

    [Fact]
    public async Task Fails_WhenEmailNotInAdminList()
    {
        var options = CreateOptions(adminEmails: ["admin@example.com"]);
        var httpContext = CreateHttpContext();

        var context = await RunHandler(options, CreateUser("other@example.com"), httpContext);

        Assert.True(context.HasFailed);
    }

    [Fact]
    public async Task Fails_WhenHttpContextIsNull()
    {
        var options = CreateOptions(adminEmails: ["admin@example.com"]);

        var context = await RunHandler(options, CreateUser("admin@example.com"), httpContext: null);

        Assert.True(context.HasFailed);
    }

    [Fact]
    public async Task Fails_WhenIpNotInAllowedRange()
    {
        var options = CreateOptions(
            adminEmails: ["admin@example.com"],
            adminIPs: ["10.0.0.0/8"]);
        var httpContext = CreateHttpContext("192.168.1.1");

        var context = await RunHandler(options, CreateUser("admin@example.com"), httpContext);

        Assert.True(context.HasFailed);
    }

    [Fact]
    public async Task Succeeds_WhenMultipleAdminEmails_MatchesOne()
    {
        var options = CreateOptions(adminEmails: ["first@example.com", "second@example.com"]);
        var httpContext = CreateHttpContext();

        var context = await RunHandler(options, CreateUser("second@example.com"), httpContext);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Succeeds_WhenMultipleIpRanges_MatchesOne()
    {
        var options = CreateOptions(
            adminEmails: ["admin@example.com"],
            adminIPs: ["10.0.0.0/8", "192.168.0.0/16"]);
        var httpContext = CreateHttpContext("192.168.1.50");

        var context = await RunHandler(options, CreateUser("admin@example.com"), httpContext);

        Assert.True(context.HasSucceeded);
    }
}

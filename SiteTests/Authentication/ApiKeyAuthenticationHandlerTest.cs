using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Site;
using Site.Authentication;
using Xunit;

namespace SiteTests.Authentication;

public class ApiKeyAuthenticationHandlerTest
{
    private class TestOptionsMonitor : IOptionsMonitor<ApiKeyAuthenticationSchemeOptions>
    {
        public ApiKeyAuthenticationSchemeOptions CurrentValue { get; } = new();
        public ApiKeyAuthenticationSchemeOptions Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<ApiKeyAuthenticationSchemeOptions, string?> listener) => null;
    }

    private static async Task<AuthenticateResult> RunHandler(
        ApiKeysOptions apiKeysOptions,
        HttpContext httpContext)
    {
        var optionsMonitor = new TestOptionsMonitor();
        var loggerFactory = NullLoggerFactory.Instance;
        var urlEncoder = UrlEncoder.Default;
        var apiKeysOptionsWrapper = Options.Create(apiKeysOptions);

        var handler = new ApiKeyAuthenticationHandler(
            optionsMonitor, loggerFactory, urlEncoder, apiKeysOptionsWrapper);

        var scheme = new AuthenticationScheme("ApiKey", null, typeof(ApiKeyAuthenticationHandler));
        await handler.InitializeAsync(scheme, httpContext);
        return await handler.AuthenticateAsync();
    }

    [Fact]
    public async Task Fails_WhenNoValidKeysConfigured()
    {
        var context = new DefaultHttpContext();
        var result = await RunHandler(new ApiKeysOptions { ValidKeys = new List<string>() }, context);

        Assert.False(result.Succeeded);
        Assert.Contains("No valid keys configured", result.Failure!.Message);
    }

    [Fact]
    public async Task Fails_WhenValidKeysIsNull()
    {
        var context = new DefaultHttpContext();
        var result = await RunHandler(new ApiKeysOptions { ValidKeys = null! }, context);

        Assert.False(result.Succeeded);
        Assert.Contains("No valid keys configured", result.Failure!.Message);
    }

    [Fact]
    public async Task Fails_WhenMissingApiKeyHeader()
    {
        var context = new DefaultHttpContext();
        var result = await RunHandler(
            new ApiKeysOptions { ValidKeys = new List<string> { "valid-key" } },
            context);

        Assert.False(result.Succeeded);
        Assert.Contains("Missing x-api-key header", result.Failure!.Message);
    }

    [Fact]
    public async Task Fails_WhenApiKeyIsEmpty()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["x-api-key"] = "";
        var result = await RunHandler(
            new ApiKeysOptions { ValidKeys = new List<string> { "valid-key" } },
            context);

        Assert.False(result.Succeeded);
        Assert.Contains("Invalid API key", result.Failure!.Message);
    }

    [Fact]
    public async Task Fails_WhenApiKeyIsInvalid()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["x-api-key"] = "wrong-key";
        var result = await RunHandler(
            new ApiKeysOptions { ValidKeys = new List<string> { "valid-key" } },
            context);

        Assert.False(result.Succeeded);
        Assert.Contains("Invalid API key", result.Failure!.Message);
    }

    [Fact]
    public async Task Succeeds_WhenApiKeyIsValid()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["x-api-key"] = "my-secret-key";
        var result = await RunHandler(
            new ApiKeysOptions { ValidKeys = new List<string> { "my-secret-key" } },
            context);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Principal);
        Assert.Equal("ApiKeyUser", result.Principal!.Identity!.Name);
    }

    [Fact]
    public async Task Succeeds_TicketContainsApiKeyClaim()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["x-api-key"] = "key-123";
        var result = await RunHandler(
            new ApiKeysOptions { ValidKeys = new List<string> { "key-123" } },
            context);

        Assert.True(result.Succeeded);
        var apiKeyClaim = result.Principal!.FindFirst("ApiKey");
        Assert.NotNull(apiKeyClaim);
        Assert.Equal("key-123", apiKeyClaim!.Value);
    }

    [Fact]
    public async Task Succeeds_WhenOneOfMultipleKeysMatches()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["x-api-key"] = "key-b";
        var result = await RunHandler(
            new ApiKeysOptions { ValidKeys = new List<string> { "key-a", "key-b", "key-c" } },
            context);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task SchemeNameIsUsedForIdentity()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["x-api-key"] = "valid";
        var result = await RunHandler(
            new ApiKeysOptions { ValidKeys = new List<string> { "valid" } },
            context);

        Assert.True(result.Succeeded);
        Assert.Equal("ApiKey", result.Ticket!.AuthenticationScheme);
        Assert.Equal("ApiKey", result.Principal!.Identity!.AuthenticationType);
    }
}

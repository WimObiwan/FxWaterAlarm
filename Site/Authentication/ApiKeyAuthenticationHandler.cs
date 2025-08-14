using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Site.Authentication;

public class ApiKeyAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
}

public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationSchemeOptions>
{
    private readonly ApiKeysOptions _apiKeysOptions;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptions<ApiKeysOptions> apiKeysOptions
    ) : base(options, logger, encoder)
    {
        _apiKeysOptions = apiKeysOptions.Value;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // If no valid keys are configured, all requests are unauthorized
        if (_apiKeysOptions.ValidKeys == null || _apiKeysOptions.ValidKeys.Count == 0)
        {
            Logger.LogWarning("API key authentication failed: No valid keys configured");
            return Task.FromResult(AuthenticateResult.Fail("No valid keys configured"));
        }

        // Check for x-api-key header
        if (!Request.Headers.TryGetValue("x-api-key", out var apiKeyValues))
        {
            Logger.LogWarning("API key authentication failed: Missing x-api-key header");
            return Task.FromResult(AuthenticateResult.Fail("Missing x-api-key header"));
        }

        var apiKey = apiKeyValues.FirstOrDefault();
        if (string.IsNullOrEmpty(apiKey) || !_apiKeysOptions.ValidKeys.Contains(apiKey))
        {
            Logger.LogWarning("API key authentication failed: Invalid API key");
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key"));
        }

        // Create a claims principal for the authenticated API key
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "ApiKeyUser"),
            new Claim("ApiKey", apiKey)
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        Logger.LogDebug("API key authentication succeeded");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
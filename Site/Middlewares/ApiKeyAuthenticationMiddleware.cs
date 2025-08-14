using Microsoft.Extensions.Options;

namespace Site.Middlewares;

public static class ApiKeyAuthenticationMiddlewareExtensions
{
    public static IApplicationBuilder UseApiKeyAuthentication(this IApplicationBuilder app)
    {
        app.UseMiddleware<ApiKeyAuthenticationMiddleware>();
        return app;
    }
}

public class ApiKeyAuthenticationMiddleware : IMiddleware
{
    private readonly ApiKeysOptions _apiKeysOptions;

    public ApiKeyAuthenticationMiddleware(IOptions<ApiKeysOptions> apiKeysOptions)
    {
        _apiKeysOptions = apiKeysOptions.Value;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // Only apply authentication to /api/deveui routes
        if (context.Request.Path.StartsWithSegments("/api/deveui"))
        {
            // If no valid keys are configured, all requests are unauthorized
            if (_apiKeysOptions.ValidKeys == null || _apiKeysOptions.ValidKeys.Count == 0)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized: API key authentication is required but no valid keys are configured");
                return;
            }

            // Check for x-api-key header
            if (!context.Request.Headers.TryGetValue("x-api-key", out var apiKeyValues))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized: Missing x-api-key header");
                return;
            }

            var apiKey = apiKeyValues.FirstOrDefault();
            if (string.IsNullOrEmpty(apiKey) || !_apiKeysOptions.ValidKeys.Contains(apiKey))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized: Invalid API key");
                return;
            }
        }

        await next(context);
    }
}
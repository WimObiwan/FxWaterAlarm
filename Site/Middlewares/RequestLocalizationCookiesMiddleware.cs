using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;

namespace Site.Middlewares;

public static class RequestLocalizationCookiesMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLocalizationCookies(this IApplicationBuilder app)
    {
        app.UseMiddleware<RequestLocalizationCookiesMiddleware>();
        return app;
    }
}

public class RequestLocalizationCookiesMiddleware : IMiddleware
{
    public RequestLocalizationCookiesMiddleware(IOptions<RequestLocalizationOptions> requestLocalizationOptions)
    {
        Provider =
            requestLocalizationOptions
                .Value
                .RequestCultureProviders
                .Where(x => x is CookieRequestCultureProvider)
                .Cast<CookieRequestCultureProvider>()
                .FirstOrDefault();
    }

    private CookieRequestCultureProvider? Provider { get; }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (Provider != null)
        {
            var feature = context.Features.Get<IRequestCultureFeature>();

            if (feature != null)
                // remember culture across request
                context.Response
                    .Cookies
                    .Append(
                        Provider.CookieName,
                        CookieRequestCultureProvider.MakeCookieValue(feature.RequestCulture)
                    );
        }

        await next(context);
    }
}
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using Site.Middlewares;
using Xunit;

namespace SiteTests.Middlewares;

public class RequestLocalizationCookiesMiddlewareTest
{
    [Fact]
    public async Task InvokeAsync_SetsCookie_WhenProviderAndFeatureExist()
    {
        var cookieProvider = new CookieRequestCultureProvider();
        var localizationOptions = Options.Create(new RequestLocalizationOptions
        {
            RequestCultureProviders = new List<IRequestCultureProvider> { cookieProvider }
        });
        var middleware = new RequestLocalizationCookiesMiddleware(localizationOptions);

        var context = new DefaultHttpContext();
        var requestCulture = new RequestCulture("nl");
        var feature = new RequestCultureFeature(requestCulture, cookieProvider);
        context.Features.Set<IRequestCultureFeature>(feature);

        bool nextCalled = false;
        await middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.True(nextCalled);
        Assert.True(context.Response.Headers.ContainsKey("Set-Cookie"));
    }

    [Fact]
    public async Task InvokeAsync_DoesNotSetCookie_WhenNoProvider()
    {
        var localizationOptions = Options.Create(new RequestLocalizationOptions
        {
            RequestCultureProviders = new List<IRequestCultureProvider>()
        });
        var middleware = new RequestLocalizationCookiesMiddleware(localizationOptions);

        var context = new DefaultHttpContext();
        bool nextCalled = false;
        await middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.True(nextCalled);
        Assert.False(context.Response.Headers.ContainsKey("Set-Cookie"));
    }

    [Fact]
    public async Task InvokeAsync_DoesNotSetCookie_WhenNoFeature()
    {
        var cookieProvider = new CookieRequestCultureProvider();
        var localizationOptions = Options.Create(new RequestLocalizationOptions
        {
            RequestCultureProviders = new List<IRequestCultureProvider> { cookieProvider }
        });
        var middleware = new RequestLocalizationCookiesMiddleware(localizationOptions);

        var context = new DefaultHttpContext();
        // Do not set IRequestCultureFeature

        bool nextCalled = false;
        await middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.True(nextCalled);
        Assert.False(context.Response.Headers.ContainsKey("Set-Cookie"));
    }

    [Fact]
    public async Task InvokeAsync_AlwaysCallsNext()
    {
        var localizationOptions = Options.Create(new RequestLocalizationOptions());
        var middleware = new RequestLocalizationCookiesMiddleware(localizationOptions);

        var context = new DefaultHttpContext();
        bool nextCalled = false;
        await middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.True(nextCalled);
    }
}

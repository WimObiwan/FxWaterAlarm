using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Site.Pages;
using SiteTests.Helpers;

namespace SiteTests.Pages;

public class SimplePageModelsTest
{
    [Fact]
    public void IndexModel_OnGet_DoesNotThrow()
    {
        var model = new IndexModel(NullLogger<IndexModel>.Instance);
        TestEntityFactory.SetupPageContext(model);
        model.OnGet();
    }

    [Fact]
    public void PrivacyModel_OnGet_DoesNotThrow()
    {
        var model = new PrivacyModel(NullLogger<PrivacyModel>.Instance);
        TestEntityFactory.SetupPageContext(model);
        model.OnGet();
    }

    [Fact]
    public void AdminOverview_OnGet_DoesNotThrow()
    {
        var model = new AdminOverview(new FakeAuthorizationService());
        TestEntityFactory.SetupPageContext(model);
        model.OnGet();
    }

    [Fact]
    public void AdminQr_OnGet_SetsQrUrl()
    {
        var model = new AdminQr();
        TestEntityFactory.SetupPageContext(model);
        model.OnGet("https://example.com/qr");
        Assert.Equal("https://example.com/qr", model.QrUrl);
    }

    [Fact]
    public void AdminQr_QrUrl_DefaultsToEmpty()
    {
        var model = new AdminQr();
        Assert.Equal(string.Empty, model.QrUrl);
    }

    private class FakeAuthorizationService : Microsoft.AspNetCore.Authorization.IAuthorizationService
    {
        public Task<Microsoft.AspNetCore.Authorization.AuthorizationResult> AuthorizeAsync(
            System.Security.Claims.ClaimsPrincipal user,
            object? resource,
            System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Authorization.IAuthorizationRequirement> requirements)
            => Task.FromResult(Microsoft.AspNetCore.Authorization.AuthorizationResult.Success());

        public Task<Microsoft.AspNetCore.Authorization.AuthorizationResult> AuthorizeAsync(
            System.Security.Claims.ClaimsPrincipal user,
            object? resource,
            string policyName)
            => Task.FromResult(Microsoft.AspNetCore.Authorization.AuthorizationResult.Success());
    }
}

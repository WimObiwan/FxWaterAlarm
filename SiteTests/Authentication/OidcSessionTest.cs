using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Site.Authentication;
using Xunit;

namespace SiteTests.Authentication;

public class OidcSessionTest
{
    [Fact]
    public void StoreIdToken_RoundTripsThroughGetIdToken()
    {
        var properties = new AuthenticationProperties();

        OidcSession.StoreIdToken(properties, "the-id-token");

        Assert.Equal("the-id-token", OidcSession.GetIdToken(properties));
    }

    [Fact]
    public void StoreIdToken_IgnoresMissingToken()
    {
        var properties = new AuthenticationProperties();

        OidcSession.StoreIdToken(properties, null);

        Assert.Null(OidcSession.GetIdToken(properties));
    }

    [Fact]
    public void GetIdToken_ReturnsNull_WhenNoProperties()
    {
        Assert.Null(OidcSession.GetIdToken(null));
    }

    [Fact]
    public void SignOutOfProvider_SignsOutOfOidcWithHint_WhenIdTokenPresent()
    {
        var result = OidcSession.SignOutOfProvider("the-id-token", "/login?error=no_account");

        var signOut = Assert.IsType<SignOutResult>(result);
        Assert.Equal([OidcSession.Scheme], signOut.AuthenticationSchemes);
        Assert.Equal("/login?error=no_account", signOut.Properties!.RedirectUri);
        Assert.Equal("the-id-token", signOut.Properties.Items[OidcSession.IdTokenHintItem]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void SignOutOfProvider_JustRedirects_WhenNoIdToken(string? idToken)
    {
        var result = OidcSession.SignOutOfProvider(idToken, "/login?error=no_account");

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/login?error=no_account", redirect.Url);
    }
}

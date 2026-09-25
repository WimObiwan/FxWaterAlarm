using Microsoft.IdentityModel.Tokens;
using Site.Authentication;
using Xunit;

namespace SiteTests.Authentication;

public class KeycloakRolesTest
{
    private static string CreateToken(string payloadJson)
    {
        var header = Base64UrlEncoder.Encode("""{"alg":"RS256","typ":"JWT"}""");
        var payload = Base64UrlEncoder.Encode(payloadJson);
        return $"{header}.{payload}.c2lnbmF0dXJl";
    }

    [Fact]
    public void ReadClientRoles_ReturnsRolesOfOwnClient()
    {
        var token = CreateToken("""
            {"resource_access":{"wateralarm-dev":{"roles":["admin","other"]},"filmoptv":{"roles":["edit"]}}}
            """);

        var roles = KeycloakRoles.ReadClientRoles(token, "wateralarm-dev");

        Assert.Equal(["admin", "other"], roles);
    }

    [Fact]
    public void ReadClientRoles_IgnoresRealmRoles()
    {
        var token = CreateToken("""{"realm_access":{"roles":["admin"]}}""");

        Assert.Empty(KeycloakRoles.ReadClientRoles(token, "wateralarm-dev"));
    }

    [Fact]
    public void ReadClientRoles_IgnoresRolesOfOtherClient()
    {
        var token = CreateToken("""{"resource_access":{"wateralarm-prd":{"roles":["admin"]}}}""");

        Assert.Empty(KeycloakRoles.ReadClientRoles(token, "wateralarm-dev"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("opaque-token")]
    public void ReadClientRoles_ReturnsEmpty_WhenNotAJwt(string? token)
    {
        Assert.Empty(KeycloakRoles.ReadClientRoles(token, "wateralarm-dev"));
    }

    [Fact]
    public void ReadClientRoles_SkipsNonStringRoles()
    {
        var token = CreateToken("""{"resource_access":{"wateralarm-dev":{"roles":["admin",42,null]}}}""");

        Assert.Equal(["admin"], KeycloakRoles.ReadClientRoles(token, "wateralarm-dev"));
    }
}

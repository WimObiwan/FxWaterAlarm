using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace Site.Authentication;

/// <summary>
/// Ends the Keycloak SSO session together with (or instead of) the WaterAlarm one.
/// Without this, a login WaterAlarm rejects, or a plain logout, leaves the Keycloak session
/// alive, and the next "Aanmelden" silently returns the same identity again.
/// </summary>
public static class OidcSession
{
    public const string Scheme = "oidc";

    /// <summary>Token name under which the id_token is kept in AuthenticationProperties.</summary>
    public const string IdTokenName = "id_token";

    /// <summary>AuthenticationProperties item carrying the id_token_hint to the sign-out redirect.</summary>
    public const string IdTokenHintItem = "oidc.id_token_hint";

    /// <summary>
    /// Keeps the id_token in the properties of a ticket. SaveTokens stays off, so this is the
    /// only token carried around: access and refresh tokens are still dropped.
    /// </summary>
    public static void StoreIdToken(AuthenticationProperties properties, string? idToken)
    {
        if (!string.IsNullOrEmpty(idToken))
            properties.StoreTokens([new AuthenticationToken { Name = IdTokenName, Value = idToken }]);
    }

    public static string? GetIdToken(AuthenticationProperties? properties)
        => properties?.GetTokenValue(IdTokenName);

    /// <summary>
    /// Logs out at Keycloak, then lands on <paramref name="redirectUri"/>. Without an id_token
    /// (email-code login, legacy Google session, a cookie from before this change) there is no
    /// Keycloak session we know of, so it just redirects.
    /// </summary>
    /// <remarks>
    /// The id_token_hint lets Keycloak skip its "Do you want to log out?" confirmation page.
    /// It is picked up in OnRedirectToIdentityProviderForSignOut (Program.cs), because the
    /// handler's own lookup reads the ExternalCookie, which is already consumed by then.
    /// </remarks>
    public static IActionResult SignOutOfProvider(string? idToken, string redirectUri)
    {
        if (string.IsNullOrEmpty(idToken))
            return new RedirectResult(redirectUri);

        var properties = new AuthenticationProperties { RedirectUri = redirectUri };
        properties.Items[IdTokenHintItem] = idToken;
        return new SignOutResult(Scheme, properties);
    }
}

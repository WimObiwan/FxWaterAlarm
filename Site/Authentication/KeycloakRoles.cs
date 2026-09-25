using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Site.Authentication;

public static class KeycloakRoles
{
    /// <summary>Client role on the wateralarm-dev / wateralarm-prd Keycloak client that grants admin.</summary>
    public const string Admin = "admin";

    // Keycloak's built-in "roles" client scope puts the user's roles in the access token only,
    // nested as realm_access.roles and resource_access.{clientId}.roles. Only the client roles
    // are read: a realm role is shared by every app in the realm, and an "admin" realm role
    // meant for another app must not make someone a WaterAlarm admin.
    // The access token came straight from the token endpoint over the authenticated back
    // channel (code flow + client secret), so its payload is trusted without re-validating it.
    public static IReadOnlyList<string> ReadClientRoles(string? accessToken, string clientId)
    {
        if (string.IsNullOrEmpty(accessToken))
            return [];

        JsonDocument payload;
        try
        {
            var jwt = new JsonWebToken(accessToken);
            payload = JsonDocument.Parse(Base64UrlEncoder.Decode(jwt.EncodedPayload));
        }
        catch (Exception e) when (e is ArgumentException or SecurityTokenMalformedException or JsonException)
        {
            // Not a JWT (opaque token): no roles to read.
            return [];
        }

        using (payload)
        {
            if (!payload.RootElement.TryGetProperty("resource_access", out var resourceAccess)
                || resourceAccess.ValueKind != JsonValueKind.Object
                || !resourceAccess.TryGetProperty(clientId, out var clientAccess)
                || clientAccess.ValueKind != JsonValueKind.Object
                || !clientAccess.TryGetProperty("roles", out var roles)
                || roles.ValueKind != JsonValueKind.Array)
                return [];

            return roles.EnumerateArray()
                .Where(r => r.ValueKind == JsonValueKind.String)
                .Select(r => r.GetString()!)
                .Distinct()
                .ToList();
        }
    }
}

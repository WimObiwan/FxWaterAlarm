namespace Site.Authentication;

public class OidcOptions
{
    public const string Location = "Oidc";

    /// <summary>
    /// OIDC issuer, e.g. https://auth.foxinnovations.be/realms/foxinnovations
    /// Discovery is read from {Authority}/.well-known/openid-configuration.
    /// </summary>
    public string? Authority { get; init; }

    public string? ClientId { get; init; }
    public string? ClientSecret { get; init; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Authority)
        && !string.IsNullOrWhiteSpace(ClientId)
        && !string.IsNullOrWhiteSpace(ClientSecret);
}

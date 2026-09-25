namespace Core.Entities;

public enum AccountUserLoginType
{
    Mail = 0,
    /// <summary>Legacy: direct Google OAuth, before the move to Keycloak. Kept so existing rows keep working.</summary>
    Google = 1,
    /// <summary>External login brokered by Keycloak (which may itself federate Google, Entra, ...).</summary>
    Oidc = 2,
}

public class AccountUser
{
    public int Id { get; init; }
    public required int AccountId { get; init; }
    public Account Account { get; init; } = null!;
    public required AccountUserLoginType LoginType { get; init; }
    /// <summary>Email address, set for LoginType.Mail</summary>
    public string? Email { get; init; }
    /// <summary>External provider name: "oidc" (Keycloak), or legacy "google"</summary>
    public string? Provider { get; init; }
    /// <summary>Provider subject ID, set for external logins</summary>
    public string? ProviderSubjectId { get; init; }
    public required DateTime CreationTimestamp { get; init; }
}

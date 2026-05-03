namespace Core.Entities;

public enum AccountUserLoginType
{
    Mail = 0,
    Google = 1,
}

public class AccountUser
{
    public int Id { get; init; }
    public required int AccountId { get; init; }
    public Account Account { get; init; } = null!;
    public required AccountUserLoginType LoginType { get; init; }
    /// <summary>Email address, set for LoginType.Mail</summary>
    public string? Email { get; init; }
    /// <summary>OAuth provider name (e.g. "google"), set for external logins</summary>
    public string? Provider { get; init; }
    /// <summary>Provider subject ID, set for external logins</summary>
    public string? ProviderSubjectId { get; init; }
    public required DateTime CreationTimestamp { get; init; }
}

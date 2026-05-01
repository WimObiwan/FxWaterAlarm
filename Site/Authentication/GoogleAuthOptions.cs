namespace Site.Authentication;

public class GoogleAuthOptions
{
    public const string Location = "GoogleAuth";

    public string? ClientId { get; init; }
    public string? ClientSecret { get; init; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
}

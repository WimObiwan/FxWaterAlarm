namespace Site.Services;

class MessagesOptions
{
    public const string Location = "BannerMessages";

    public required Message[] Messages { get; init; }
    public TimeSpan? DismissRepeatInterval { get; init; }
}

class Message
{
    public enum TypeEnum { Primary, Secondary, Success, Danger, Warning, Info, Light, Dark }

    public required string Id { get; init; }
    public required TypeEnum Type { get; init; }
    public required Dictionary<string, string> Contents { get; init; }
    public required DateTime ExpirationUtc { get; init; }
}
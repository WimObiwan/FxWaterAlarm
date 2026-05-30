namespace Core.Configuration;

public sealed class ThingsNetworkOptions
{
    public const string Location = "ThingsNetwork";

    public string ApiBaseUrl { get; init; } = string.Empty;
    public IReadOnlyList<ThingsNetworkApplicationOptions> Applications { get; init; } = [];
    public string WebhookId { get; init; } = "wateralarm-admin";
    public int DeviceLookupPageSize { get; init; } = 100;
}

public sealed class ThingsNetworkApplicationOptions
{
    public string ApplicationId { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    public string? WebhookId { get; init; }
}

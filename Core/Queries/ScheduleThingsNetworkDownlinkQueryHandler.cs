using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Core.Configuration;
using MediatR;
using Microsoft.Extensions.Options;

namespace Core.Queries;

public record ScheduleThingsNetworkDownlinkQuery : IRequest<ThingsNetworkDownlinkResult>
{
    public required string DevEui { get; init; }
    public required byte[] Payload { get; init; }
    public int FPort { get; init; } = 15;
    public bool Confirmed { get; init; }
    public string Priority { get; init; } = "NORMAL";
    public string? ApplicationId { get; init; }
    public string? WebhookId { get; init; }
}

public sealed record ThingsNetworkDownlinkResult
{
    public required string DevEui { get; init; }
    public required string ApplicationId { get; init; }
    public required string DeviceId { get; init; }
    public required string WebhookId { get; init; }
    public required int FPort { get; init; }
    public required bool Confirmed { get; init; }
    public required string Priority { get; init; }
    public required string FrmPayloadBase64 { get; init; }
}

public class ScheduleThingsNetworkDownlinkQueryHandler : IRequestHandler<ScheduleThingsNetworkDownlinkQuery, ThingsNetworkDownlinkResult>
{
    private readonly ThingsNetworkOptions _options;
    private readonly HttpClient? _httpClient;

    public ScheduleThingsNetworkDownlinkQueryHandler(IOptions<ThingsNetworkOptions> options)
        : this(options, null)
    {
    }

    internal ScheduleThingsNetworkDownlinkQueryHandler(IOptions<ThingsNetworkOptions> options, HttpClient? httpClient)
    {
        _options = options.Value;
        _httpClient = httpClient;
    }

    public async Task<ThingsNetworkDownlinkResult> Handle(ScheduleThingsNetworkDownlinkQuery request, CancellationToken cancellationToken)
    {
        if (request.Payload.Length == 0)
            throw new InvalidOperationException("The downlink payload must contain at least one byte.");

        if (request.FPort <= 0 || request.FPort > 255)
            throw new InvalidOperationException("The FPort must be between 1 and 255.");

        var apiBaseUrl = RequireValue(_options.ApiBaseUrl, nameof(ThingsNetworkOptions.ApiBaseUrl));
        var normalizedDevEui = NormalizeHex(request.DevEui);
        if (normalizedDevEui == null)
            throw new InvalidOperationException("The DevEUI value is required.");

        var configuredApplications = _options.Applications
            .Where(a => !string.IsNullOrWhiteSpace(a.ApplicationId))
            .ToArray();
        if (configuredApplications.Length == 0)
            throw new InvalidOperationException(
                $"Missing configuration value '{ThingsNetworkOptions.Location}:Applications'.");

        ThingsNetworkApplicationOptions[] candidateApplications;
        if (!string.IsNullOrWhiteSpace(request.ApplicationId))
        {
            var requestedAppId = request.ApplicationId.Trim();
            var application = configuredApplications.SingleOrDefault(a =>
                string.Equals(a.ApplicationId, requestedAppId, StringComparison.OrdinalIgnoreCase));
            if (application == null)
                throw new InvalidOperationException(
                    $"No configured application matches '{requestedAppId}' in '{ThingsNetworkOptions.Location}:Applications'.");

            candidateApplications = [application];
        }
        else
        {
            candidateApplications = configuredApplications;
        }

        var ownsHttpClient = _httpClient == null;
        var httpClient = _httpClient ?? new HttpClient();
        httpClient.BaseAddress = new Uri(apiBaseUrl, UriKind.Absolute);

        try
        {
            foreach (var application in candidateApplications)
            {
                var applicationId = RequireValue(application.ApplicationId,
                    nameof(ThingsNetworkApplicationOptions.ApplicationId));
                var apiKey = RequireValue(application.ApiKey, nameof(ThingsNetworkApplicationOptions.ApiKey));
                var webhookId = request.WebhookId
                                ?? application.WebhookId
                                ?? RequireValue(_options.WebhookId, nameof(ThingsNetworkOptions.WebhookId));

                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                var deviceId = await ResolveDeviceIdAsync(httpClient, applicationId, normalizedDevEui, cancellationToken);
                if (deviceId == null)
                    continue;

                var frmPayloadBase64 = Convert.ToBase64String(request.Payload);

                await PushDownlinkAsync(httpClient, applicationId, webhookId, deviceId, frmPayloadBase64, request.FPort,
                    request.Priority, request.Confirmed, cancellationToken);

                return new ThingsNetworkDownlinkResult
                {
                    DevEui = normalizedDevEui,
                    ApplicationId = applicationId,
                    DeviceId = deviceId,
                    WebhookId = webhookId,
                    FPort = request.FPort,
                    Confirmed = request.Confirmed,
                    Priority = request.Priority,
                    FrmPayloadBase64 = frmPayloadBase64
                };
            }

            throw new InvalidOperationException(
                $"No TTN end device was found in configured application(s) for DevEUI '{normalizedDevEui}'.");
        }
        finally
        {
            if (ownsHttpClient)
                httpClient.Dispose();
        }
    }

    private async Task<string?> ResolveDeviceIdAsync(HttpClient httpClient, string applicationId, string devEui,
        CancellationToken cancellationToken)
    {
        var page = 1;
        var pageSize = Math.Max(1, _options.DeviceLookupPageSize);

        while (true)
        {
            var requestUri =
                $"/api/v3/applications/{Uri.EscapeDataString(applicationId)}/devices?field_mask=ids&page={page}&limit={pageSize}";

            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);
            using var response = await httpClient.SendAsync(requestMessage, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException(
                    $"The Things Network device lookup failed for application '{applicationId}' ({(int)response.StatusCode}): {content}");

            using var json = JsonDocument.Parse(content);
            if (!json.RootElement.TryGetProperty("end_devices", out var devices) || devices.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException("Unexpected TTN response while resolving device_id for DevEUI.");

            var count = 0;
            foreach (var device in devices.EnumerateArray())
            {
                count++;
                if (!device.TryGetProperty("ids", out var ids))
                    continue;

                var deviceDevEui = ids.TryGetProperty("dev_eui", out var devEuiValue)
                    ? NormalizeHex(devEuiValue.GetString())
                    : null;

                if (deviceDevEui == null || !string.Equals(deviceDevEui, devEui, StringComparison.OrdinalIgnoreCase))
                    continue;

                var deviceId = ids.TryGetProperty("device_id", out var deviceIdValue)
                    ? deviceIdValue.GetString()
                    : null;

                if (string.IsNullOrWhiteSpace(deviceId))
                    break;

                return deviceId;
            }

            if (count < pageSize)
                break;

            page++;
        }

        return null;
    }

    private static async Task PushDownlinkAsync(HttpClient httpClient, string applicationId, string webhookId,
        string deviceId, string frmPayloadBase64, int fPort, string priority, bool confirmed,
        CancellationToken cancellationToken)
    {
        var requestUri =
            $"/api/v3/as/applications/{Uri.EscapeDataString(applicationId)}/webhooks/{Uri.EscapeDataString(webhookId)}/devices/{Uri.EscapeDataString(deviceId)}/down/push";

        var requestBody = new
        {
            downlinks = new[]
            {
                new
                {
                    frm_payload = frmPayloadBase64,
                    f_port = fPort,
                    priority,
                    confirmed
                }
            }
        };

        var jsonBody = JsonSerializer.Serialize(requestBody);
        using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        using var response = await httpClient.PostAsync(requestUri, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"The Things Network downlink request failed ({(int)response.StatusCode}): {error}");
        }
    }

    private static string RequireValue(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Missing configuration value '{ThingsNetworkOptions.Location}:{name}'.");

        return value.Trim();
    }

    private static string? NormalizeHex(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var chars = value.Where(char.IsLetterOrDigit).ToArray();
        return new string(chars).ToUpperInvariant();
    }
}

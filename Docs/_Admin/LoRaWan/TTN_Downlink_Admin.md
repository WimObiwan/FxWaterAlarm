# TTN Downlink (Admin)

Use the PowerShell admin cmdlet `Invoke-WATtnDownlink` to schedule a downlink via The Things Network HTTP API.

## Configuration

Set these values in `Admin/appsettings.Local.json` under `ThingsNetwork`:

- `ApiBaseUrl`: TTN cluster URL (for example `https://eu1.cloud.thethings.network`)
- `Applications[]`: one entry per TTN application
- `Applications[].ApplicationId`: TTN application id
- `Applications[].ApiKey`: app-specific TTN API key with downlink and device read rights
- `Applications[].WebhookId` (optional): per-application webhook namespace for downlink endpoint
- `WebhookId`: global fallback webhook namespace (default `wateralarm-admin`)

When `-ApplicationId` is omitted, the cmdlet searches each configured application for the matching DevEUI and uses the API key of the application where it is found.

## Examples

```powershell
# Set update interval to 5 minutes (0100012C)
Invoke-WATtnDownlink -DevEui A801234567890123 -PayloadHex 0100012C

# Set update interval to 20 minutes (010004B0)
Invoke-WATtnDownlink -DevEui A801234567890123 -PayloadHex 010004B0

# Set update interval to 2 hours (01001C20)
Invoke-WATtnDownlink -DevEui A801234567890123 -PayloadHex 01001C20

# Override the TTN app/webhook when needed
Invoke-WATtnDownlink -DevEui A801234567890123 -PayloadHex 0100A8C0 -ApplicationId fx-waterlevel -WebhookId wateralarm-admin

# Confirmed downlink on custom port
Invoke-WATtnDownlink -DevEui A801234567890123 -PayloadHex 01015180 -FPort 15 -Confirmed
```

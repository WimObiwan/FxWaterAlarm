The TTN downlink API is keyed on `application_id` + `device_id`, not DevEUI, so the work splits into "find the device" then "schedule the message."

**1. Create an API key** in the Console under your application → API Keys, with at least:
- *Read application traffic (uplink, downlink)*
- *Write downlink application traffic*
- *Read end device information* (so you can resolve the DevEUI)

**2. Resolve DevEUI → device_id.** TTN doesn't expose a global "find by DevEUI" for arbitrary callers — the search is scoped to an application. Two practical options:
- If you know the application: `GET /api/v3/applications/{application_id}/devices/{device_id}` to list/filter devices and pick the one whose `ids.dev_eui` matches. The list endpoint accepts a field mask so you can pull just `ids` cheaply.
- If you don't, use the Identity Server's `EndDeviceRegistrySearch` (`GET /api/v3/search/end-devices`) with a query on `dev_eui` — this requires user-level rights, not just an app API key. The `ttn-lw-cli end-devices search --dev-eui …` command does the same thing and is the fastest way to discover it interactively.

You'll get back `application_ids.application_id` and `device_id` — keep both.

**3. Schedule the downlink.** Two transports, pick one:

*HTTP* — POST to the Application Server. Three variants depending on queue behavior: `/api/v3/as/applications/{application_id}/webhooks/{webhook_id}/devices/{device_id}/down/push` (or `/replace`), with the bearer token. Body looks like:
```json
{ "downlinks": [{ "frm_payload": "vu8=", "f_port": 15, "priority": "NORMAL", "confirmed": false }] }
```
`frm_payload` is base64-encoded raw bytes. Alternatively you can send `decoded_payload` and let the device's downlink payload formatter encode it. Note the `{webhook_id}` segment: even when you're not using a webhook to deliver uplinks, the AS HTTP downlink path is conventionally namespaced under a webhook ID (any string works; it's mostly a label for traceability).

*MQTT* — connect to `<region>.cloud.thethings.network:8883` with the application ID as username and the API key as password, then publish to `v3/{application_id}@{tenant_id}/devices/{device_id}/down/push`. Same JSON body. This is usually nicer if you're already keeping a persistent connection for uplinks.

**4. Confirm it queued.** Subscribe to the device's events stream (MQTT topic `…/devices/{device_id}/events/down/#` or the AS events API) and watch for `as.down.data.receive` followed by `ns.down.data.schedule.attempt` / `…success`. A class-A device only receives in the RX1/RX2 window after its next uplink, so the message may sit in queue for a while — that's normal, not an API failure.

One gotcha worth flagging given the WaterAlarm context: if the device uses `skip_payload_crypto`, you must send `frm_payload` already encrypted with the AppSKey; otherwise the AS handles encryption for you and raw bytes are fine.
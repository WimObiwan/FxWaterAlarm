# PS-NB-I10 (10 m pressure probe) reads −1250 mm

## Problem

First Dragino PS-NB-I10 (10 m cable), device ID `F860915082514413`, sends to production.
WaterAlarm shows −1250 mm. The PS-NB-I5 (5 m) units read fine.

## Checked

- `journalctl -u iotrouter` on server3 (clock Europe/Brussels), 2026-09-26 18:01:54: one uplink
  (logged twice), parsed as `pressure = 0, distance = -1250.000`. Stored as-is in InfluxDB. `[verified]`
- Parser: `IotRouter/Parsers/DraginoUdp.cs` `ParsePSNB` (repo `~/archive-small/Projects/dotnetcore/IotRouter`). `[verified]`
- Byte comparison with a working 5 m unit (`F860631074843179`), same day. `[verified]`
- Dragino PS-NB manual (wiki.dragino.com/docs/NB-IoT/flow-pressure-weight-sensors/ps-nb/). `[doc]`

## Findings

1. **−1250 is just 0 mA through the 5 m formula**: (0 − 4) / 16 × 5000 = −1250. The app is
   not at fault; IotRouter writes −1250 to InfluxDB. `[verified]`
2. **Newer firmware has a different payload layout.** Bytes after the 16-byte IMEI+IMSI:

   | offset | 5 m unit, ver `0x017C` (old layout) | 10 m unit, ver `0x0182` (new layout, per manual "since v1.3") |
   |---|---|---|
   | 16–17 | version | version |
   | 18–19 | battery mV | battery mV |
   | 20 | signal | signal |
   | 21–22 | IN1, IN2 | **idc_alarm, vdc_alarm** (new) |
   | 23–24 | EXTI level, flag | IN1, IN2 |
   | 25–26 | probe model | EXTI level, flag |
   | 27–28 | **IDC µA** | probe model |
   | 29–30 | VDC mV | **IDC µA** |
   | 31–34 | timestamp | VDC mV (31–32), timestamp 33–36 |

   `ParsePSNB` has the old offsets hard-coded, so on the new unit it reads the probe-model
   bytes (`0000`) as IDC. The real IDC is `0x0FA4` = 4004 µA = 4.004 mA → ≈ 0 mm, which fits a
   probe that isn't submerged yet. The timestamp confirms the shift: `0x6AB7EC66` sits at 33.
   The old-offset read (31) gives `0x00006AB7`, which the plausibility check rejects. `[verified]`
3. **The 5 m range is hard-coded too** (`* 5000.0m`). Even with offsets fixed, the 10 m probe
   would read half the real depth. Both units report probe model `0x0000` (AT+PROBE not set).
   Per the manual, `AT+PROBE=00bb` means water-depth mode with a range of bb metres. `[verified]` / `[doc]`
4. Parser dispatch checks `payload[16] == 0x01`, which is the high byte of the version, not a
   type. It works by accident for both firmwares. `[inferred]`

## Fix (IotRouter, 2026-09-26, uncommitted, not deployed)

- `ParsePSNB` picks the layout from the version at bytes 16–17: `>= 0x0182` uses the new layout
  (+2 bytes), otherwise the old one. `[verified]` by tests
- Probe range from the probe model: `0000` (not configured) and `0005` give 5 m, `000A` gives 10 m.
  Any other value logs a warning and falls back to 5 m. `[verified]` by tests
- `IotRouterTests/PSNBTest.cs`: theories for old and new firmware × probe `0000`/`0005`/`000A`,
  the new one using the real payload from `F860915082514413`. 25/25 tests pass.
- Replay: all 3845 unique webhook payloads in the server3 journal (since 2026-09-15) went through
  the old parser (HEAD) and the new one. Only 3 outputs differ, all from `F860915082514413`
  (−1250 → ≈0 mm). The only other NB-IoT pressure sensor in prod (`F860631074843179`, fw `0x017C`)
  and all DDS75 units are unchanged. `[verified]`
- Uplinks at 16:31/16:32 UTC, sent after AT+PROBE was reportedly set, still carry probe model
  `0000` at bytes 27–28. `[verified]`
- **AT+PROBE=000A stops the unit from sending.** With it set, nothing from `F860915082514413`
  reached the webhook bridge between 18:32 and 18:59 (Brussels). After reverting to `0000` the 18:59 uplink
  arrived. `AT+CFG` shows the value as `000a`, even when entered as `000A`. `[verified]` (bridge log + owner at the device)
  So the probe model can't be used on this firmware; the unit stays on `0000`.
- Workaround: per-device range in the `dragino-udp` parser config, which wins over the probe model:
  `"Config": { "ProbeRangeMm": { "F860915082514413": 10000 } }` on the parser type entry.
  `MergedConfigurationSection` now returns null instead of throwing when neither type nor route has a Config.
  31/31 tests pass; replay of the 3845 journal payloads is identical to before with an empty config. `[verified]`
- Still to do: deploy IotRouter + add the config entry on server3; remove the −1250 points; report to Dragino.

## Open questions

- Version numbers are decimal digits in hex: `0x017C` = 380 = v3.8.0, `0x0182` = 386 = v3.8.6 `[inferred]`.
  The layout changed somewhere in between, and the exact version is unknown. A unit on v3.8.1–v3.8.5
  would be misparsed.
- The −1250 point already in InfluxDB for `F860915082514413` probably needs removing.

## Addendum 2026-09-26 evening — deployed

- IotRouter deployed to server3 at 19:18:10 (Brussels) via `IotRouter/scripts/deploy-iotrouter-prd.sh`
  (gitignored wrapper around `deploy.sh`). Snapshot for rollback: `/opt/IotRouter-20260926`.
  `ProbeRangeMm` for `F860915082514413` added to `/opt/IotRouter/appsettings.json`. `[verified]`
- Other nodes after deploy: `F860631074843179` (5 m, old layout) read 9.245 mA / 1639 mm at 20:54, the same
  as before the deploy. DDS75 NB nodes and LoRaWAN sensors arrived and parsed normally. `F867787054436393`
  missed its 20:18 slot, but it had already missed several slots earlier that day. `[verified]`
- 10 m unit: silent between 18:59 and 22:18 (Brussels) although the device reported successful sends;
  nothing reached the bridge. The 22:18:47 uplink parsed as 4.002 mA → 1.25 mm (10 m range) and was written
  to InfluxDB. `[verified]` Cause of the gap unknown. It fits a 1NCE ESM back-off
  (see `2026-09-12-nbiot-backoff-timers.md`), but that is unconfirmed because there was no `AT+QESMC?` capture. `[hypothesis]`
- The four −1250 points (16:01:54, 16:31:08, 16:32:06, 16:59:07 UTC) were deleted with `Remove-WAMeasurement`
  from `/opt/wateralarm-admin` (`pwsh-preview`). `Get-WAMeasurements` afterwards shows only
  20:18:40 UTC, 1 mm. `[verified]`
- Correction: the sensor *is* registered (Sensor 55, LevelPressure, created 2026-09-26 15:34 UTC). Earlier
  "not in the Sensor table" came from querying the stale pre-rename DB at
  `/var/www/wateralarm.foxinnovations.be/`. Prod runs from `/var/www/www.wateralarm.be/`. `[verified]`

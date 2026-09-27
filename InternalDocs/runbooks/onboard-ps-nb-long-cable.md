# Onboard a PS-NB with a 10 m or 15 m probe

The PS-NB-I10 / -I15 variants have a probe with a 10 m / 15 m range, not just a longer cable.
IotRouter (`DraginoUdp.ParsePSNB`) converts the 4–20 mA reading with a 5 m range unless told
otherwise. Skip this and the sensor reads **half (or a third) of the real depth**.
Background: [`../state/2026-09-26-ps-nb-10m-wrong-reading.md`](../state/2026-09-26-ps-nb-10m-wrong-reading.md).

## Steps

1. **Do not set `AT+PROBE`** on the device. On firmware `0x0182` (v3.8.6), `AT+PROBE=000A`
   stops the unit from sending. Leave it at `0000`. `[verified 2026-09-26]`
2. Add the device ID (IMEI-based, e.g. `F860915082514413`) to the `dragino-udp` parser config in
   `/opt/IotRouter/appsettings.json` on server3:
   `"Config": { "ProbeRangeMm": { "<device id>": 10000 } }` (15000 for a 15 m probe).
   This setting wins over the probe model in the payload.
3. Restart IotRouter and send a test uplink (button 1–3 s). Check `journalctl -u iotrouter`
   for the parsed `distance`: in air it should be ≈ 0 mm, not −1250 (that is 0 mA read with the
   old offsets).
4. Test in a bucket: lower the probe a known depth and check that `Hoogte` in the app matches.

## Notes

- Newer firmware (`>= 0x0182`) uses a different payload layout. The parser detects this from
  the version bytes. A unit on v3.8.1–v3.8.5 is untested.
- The customer-facing side (button, cable, test) is in `Docs/Sensor_Nodes/PS-NB.md` and
  `Docs/Sensor_Nodes/Druksensor_Opstelling.md`.

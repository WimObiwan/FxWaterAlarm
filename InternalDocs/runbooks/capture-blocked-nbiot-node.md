# Capture a blocked NB-IoT node

**Status: unvalidated.** Written 2026-09-25 for first use on 2026-09-26. Built on the
hypothesis in [`../state/2026-09-12-nbiot-backoff-timers.md`](../state/2026-09-12-nbiot-backoff-timers.md);
correct it from experience after the first run.

Use when a Dragino `-NB` node has stopped delivering for days and you can physically reach
it. The goal is **evidence first, repair second** — the node has been silent for a week, so
another hour changes nothing, but one careless power cycle can destroy the only copy of the
answer.

## Bring

- Laptop, USB-TTL adapter, jumper wires (the usual blue/green/black set)
- `minicom` is already configured: `/etc/minicom/minirc.ndds75`, 9600 8N1 on `/dev/ttyUSB0`
- Node console password: `12345678`
- A phone with signal, to confirm the site has coverage independently

## Golden rules

1. **Do not disconnect the battery.** Not before the capture, not for transport.
2. **Do not press the node button.** One short press forces an extra measurement, five short
   presses power the node off — either can change the state you came to read.
3. **Do not reset, reboot or `ATZ`** until the capture is saved.
4. Note the **exact UTC time** you arrive and the **IMEI/DevEUI** of the node you are at.
   Two nodes were blocked on 2026-09-25 (`F867787054209469`, `F867787054545078`); which one
   this is matters when matching against the platform.

## Step 1 — capture, before touching anything

Connect UART with the battery still attached (JP6: pin 2 = node RX ← USB TX, pin 1 = node TX
→ USB RX, pin 0 = GND), then start minicom **with logging on**:

```bash
minicom -C ~/nbiot-capture-$(date -u +%Y%m%dT%H%M%SZ).log ndds75
```

Enter `12345678`, then `ATE1`, then run these **in this order**. The first one is the whole
point of the trip:

```
AT+QESMC?          <-- ESM reject cause. THE command. Expect +QESMC: 1,0,<cause>
AT+QEMMS?          <-- EMM main/sub state
AT+QEMMTIMER?      <-- T3346/T3448/T3412 + remaining seconds (T3396 is NOT here)
AT+CEREG=5
AT+CEREG?          <-- registration state + <cause_type>,<reject_cause>
AT+CGATT?          <-- attached?
AT+CGACT?          <-- is there a PDP context?
AT+CGPADDR         <-- did we get an IP?
AT+COPS?           <-- which PLMN (1NCE roams; the back-off is per-PLMN)
AT+QENG=3          <-- PLMN status
AT+QENG=0          <-- serving/neighbour cell
AT+CESQ            <-- signal quality
AT+QCCID           <-- ICCID, to match the 1NCE portal
AT+CGMR            <-- module firmware version
AT+CFG             <-- node-side config (TDC, CSQTIME, server address)
```

**If `AT+QESMC?` returns `ERROR`**, the console is not reaching the BC660K and you need the
module's own UART pins instead — see the Dragino wiki page *UART Access for NB ST-BC660K-GL*.
Everything else in the list is worthless without this one, so solve it before moving on.

### Reading the result

- `+QESMC: 1,0,26` (or 27, 33, …) — ESM reject cause present. **That is the answer**: the
  session is being refused by the network and T3396 is running. Cause 26 = insufficient
  resources, 27 = unknown/missing APN, 33 = service option not subscribed.
- `AT+CGATT?` = 1 with `AT+CGACT?` showing no active context — attached but no session,
  matching what 1NCE reported on 2026-09-25. Confirms the block is at ESM, not EMM.
- `AT+QEMMTIMER?` showing everything stopped — expected, and *not* evidence of "no back-off".
  T3396 is simply not reportable on this module.

## Step 2 — the decisive experiment, which needs the node in hand

This is the test we could not run in the field, and it settles the question the whole
investigation has been stuck on: **does the back-off advance in wall-clock time, or only
while the modem is powered?**

Keep the module **continuously powered** on the bench and poll until it recovers:

```
AT+CFUN=1
```
then every minute, logging with timestamps:
```
AT+CGATT?
AT+CGACT?
AT+QESMC?
```

A one-line poller is enough — anything that stamps each response with the UTC time.

| Time to recover under continuous power | Verdict |
| -------------------------------------- | ------- |
| **A few hours** | Duty-cycle model (Bram). The timer only advances while the modem is powered; the node's 2 h sleep cycle is what stretches hours into days. Fix is firmware: hold `CFUN=4` until expiry instead of cutting power. |
| **Same wall-clock moment it would have recovered anyway** (i.e. ~166–226 h after it blocked) | Wall-clock model. The duty cycle is irrelevant; the outage length is whatever the network signalled. Fix is operator-side. |

Field durations to compare against: 166.3, 196.0, 200.1, 226.1 h. `F867787054209469` blocked
at 2026-09-18 16:56:36Z, `F867787054545078` at 2026-09-20 20:27:47Z — so under the wall-clock
model both are already deep into the window and may recover on their own during the test.
**Note the exact recovery time to the second**; that number is the whole experiment.

Leave the node powered and logging even after it recovers — a second block often follows
immediately (`F867787054545078` managed exactly one uplink on 2026-09-20 before going dark
again), and catching the re-block live would show the reject arriving.

## Step 3 — only now, try to fix it

In increasing order of destructiveness, re-running `AT+QESMC?` after each:

1. `AT+CFUN=0` then `AT+CFUN=1` — detach/reattach without losing NVRAM.
2. `AT+COPS=0` — let it reselect PLMN. If the block is per-PLMN, another roaming partner may
   work, and that is itself a finding.
3. `AT+QCSEARFCN` — clear the stored EARFCN list, then reattach.
4. Power cycle. **Last resort**, and only once the capture is saved: per 3GPP the timer
   survives it anyway, and if it does *not* survive, that is a finding worth having recorded.

## Afterwards

Write the session log into `state/` as a dated finding, append the outcome to
`2026-09-12-nbiot-backoff-timers.md`, and correct this runbook where it was wrong. If
`AT+QESMC?` gave a cause code, send it to 1NCE with the ICCID and the UTC window — that is
the exact thing they were unable to give us from the portal.

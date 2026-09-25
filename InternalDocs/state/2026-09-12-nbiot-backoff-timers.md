# NB-IoT nodes silent for ~7 days — back-off timer hypothesis

## Problem

A handful of NB-IoT installations (all Dragino, all Quectel BC660K-GL) occasionally stop
delivering measurements. Roughly 5 occurrences across ~10 sensors in 3 years. Each outage
lasts about 7 days and then resolves by itself, with no intervention at the node.

Trigger for this note: a customer with NB-IoT experience (Bram) wrote:

> Ik heb het vermoeden dat we een back-off timer gekregen hebben. In de BC660 AT command
> guide moet je eens zoeken naar emm timers. 1 ervan is het netwerk die zegt dat je even
> niet mag connecteren. Het beste dat je dan kan doen is de tijd uitzitten in CFUN=4
> voordat je de modem af legt.

## What was checked

- Quectel *BC660K-GL & BC950K-GL AT Commands Manual V1.3* (PDF from quectel.com), read in
  full for the EMM/ESM diagnostic commands.
- 3GPP timer encodings (TS 24.008 §10.5.7.4 / §10.5.7.4a) and T3396 switch-off behaviour
  (TS 24.301) — via secondary sources, **not** read in the spec itself.
- WaterAlarm alarm code: `Core/Entities/AccountSensorAlarm.cs`,
  `Core/Commands/CheckAccountSensorAlarmsCommandHandlerBase.cs`,
  `Core/Commands/AddDefaultAccountSensorAlarmsCommandHandlerBase.cs`.
- No node was instrumented. Nothing below is measured on our own hardware.

## Findings

### 1. The suspected timer is T3396, not T3346 — duration is the discriminator

*(read from documentation; arithmetic is mine)*

There are two separate back-off timers and only one of them can produce a week:

| Timer | Layer | Triggered by | Encoding | Max |
| ----- | ----- | ------------ | -------- | --- |
| **T3346** | EMM (mobility) | Attach / TAU / Service Request reject, EMM cause #22 congestion | GPRS Timer 2, 5-bit value × {2 s, 1 min, 6 min} | **~186 min (3.1 h)** |
| **T3396** | ESM (session) | PDN Connectivity Reject, ESM cause #26 insufficient resources, #27 unknown APN, #33 service not subscribed | GPRS Timer 3, 5-bit value × {2 s, 30 s, 1 min, 10 min, 1 h, 10 h, 320 h} | days–months |

T3346 tops out at ~3 hours, so it cannot explain a 7-day outage. T3396 can, and the
10-hour unit lands suspiciously close: **17 × 10 h = 170 h = 7.08 days**. That is the only
encoding in GPRS Timer 3 that produces "about a week" — 1-hour units cap at 31 h and the
next unit up is 320 h. This is a hypothesis from the encoding table, not a measurement.

T3396 is scoped **per APN per PLMN**, which fits an outage that hits one subscriber while
the rest of the fleet keeps reporting.

### 2. A power cycle does not clear T3396 — by design

*(read from documentation, secondary source; not verified in the spec text)*

3GPP requires the UE to persist T3396 across switch-off: if the module is powered down
while the timer runs and the same USIM is present at power-up, it continues counting the
residual value and must not send another PDN CONNECTIVITY REQUEST for that APN until it
expires.

This is the important consequence for us: Dragino's firmware power-cycles the BC660K
around each uplink, and that **does not help**. The node comes up, restores the timer from
NVRAM, silently declines to set up the PDN connection, and goes back to sleep. It will do
that every TDC for the whole week. That matches the observed shape exactly — a fixed
multi-day silence that ends on its own, with no error visible anywhere at the node.

It also reframes Bram's advice. Parking in `CFUN=4` is not what makes the timer expire
(it expires either way); it avoids burning battery on a week of futile attach attempts,
and avoids the risk that a fresh attach attempt earns a *new*, re-randomised back-off from
a network that is still congested.

### 3. BC660K-GL: `AT+QEMMTIMER` will not show this timer

*(verified by reading the manual; not run on a device)*

`AT+QEMMTIMER?` reports **only three** timers:

```
+QEMMTIMER: <timerID>,<timer_state>[,<remain_time_value>]
  timerID  0 = T3346, 1 = T3448, 2 = T3412 or T3412_EXT
  state    0 = start, 1 = stop, 2 = expire
  remain   seconds, only present when state = 0
```

T3396 is **not** in that list. The command that exposes the ESM side is `AT+QESMC?`:

```
+QESMC: <rejCausePresent>,<causeType>,<rejCauseValue>
```

The manual's own example is literally `+QESMC: 1,0,26` — ESM cause 26, insufficient
resources, i.e. the classic T3396 trigger. So Bram's pointer is right in direction, but
the EMM timer command alone will come back empty and look like "no back-off".

Other useful commands on this module, all confirmed present in V1.3 of the manual:

| Command | Gives |
| ------- | ----- |
| `AT+CEREG=5` then `AT+CEREG?` | registration state plus `<cause_type>,<reject_cause>` (EMM cause) |
| `AT+QEMMS?` | EMM main state / substate, e.g. `EMM_DEREGISTERED`,`ATTEMPTING_TO_ATTACH` |
| `AT+QESMC?` | ESM reject cause — the T3396 evidence |
| `AT+QEMMTIMER?` | T3346 / T3448 / T3412 state + remaining seconds |
| `AT+CGATT?` | attached yes/no |
| `AT+COPS?` | selected PLMN (which roaming partner we are on) |
| `AT+QENG=3` | PLMN status; `AT+QENG=0` gives serving/neighbour cell radio info |
| `AT+CESQ` | signal quality (RSRP/RSRQ) |
| `AT+CGPADDR` | whether a PDP address was actually assigned |
| `AT+QCCID` | ICCID, to tie the capture to the SIM in the 1NCE portal |
| `AT+CFUN=4` | "disable RF transmitting and receiving" — the parking state Bram suggests; `<fun>` 0/1/4 are the supported values |

UART access to the module on Dragino `-NB` nodes is documented at
<https://wiki.dragino.com/xwiki/bin/view/Main/UART_Access_for_NB_ST_BC660K-GL/>.

### 4. Platform-side detection already exists

*(verified by reading the code)*

`AccountSensorAlarmType.Data` is a "no measurement for N hours" alarm —
`CheckAccountSensorAlarmsCommandHandlerBase.cs:82` compares `UtcNow - measurement.Timestamp`
against the threshold in hours. `AddDefaultAccountSensorAlarmsCommandHandlerBase.cs:33`
creates it by default at **24.5 h**. So a week-long silence is detected, provided the alarm
actually exists on those account-sensors — worth auditing, since it is only created by the
"add default alarms" path.

## Next time it happens — capture before touching anything

Order matters. A reset destroys `AT+QESMC?` state, and if this really is T3396 the reset
buys nothing anyway.

1. Open the node, connect USB-TTL to the BC660K UART (see Dragino wiki link above).
2. Run, in this order, and save the full transcript with a timestamp:
   `AT+CEREG=5` → `AT+CEREG?` → `AT+QEMMS?` → `AT+QESMC?` → `AT+QEMMTIMER?` →
   `AT+CGATT?` → `AT+COPS?` → `AT+QENG=3` → `AT+QENG=0` → `AT+CESQ` → `AT+CGPADDR` →
   `AT+QCCID`
3. Note the battery voltage before and after the outage — a week of retries at every TDC
   should be visible.
4. Only then decide whether to park the modem in `AT+CFUN=4` or leave it.
5. Cross-check the 1NCE portal for the same SIM and the same window.

Promote this to `runbooks/` once it has actually been used on a live incident — right now
it is an untested procedure built on a hypothesis.

## Open questions

- **Were the ~5 events single-node or fleet-wide?** Decisive. Simultaneous gaps across
  several sensors point at network congestion control; isolated single-node gaps point at
  something subscriber- or APN-specific. Answerable from InfluxDB without touching any
  hardware — find every gap > 48 h per sensor and check for overlap.
- **Were the durations all ≈170 h?** If they cluster tightly around 170 h rather than
  scattering between 5 and 9 days, that is strong evidence for T3396 = 17 × 10 h.
- **Which PLMN were the affected nodes on?** 1NCE roams; the back-off is per-PLMN, so a
  single roaming partner could be responsible.
- **Does 1NCE see the reject?** They have the attach/session logs and can say which element
  issued it, with which cause, and what T3396 value was signalled. Worth a ticket with the
  ICCID and the exact UTC window of one past outage.
- **Does Dragino's firmware honour the back-off, or does it hammer?** Unknown. Ask them,
  with a capture attached, whether the node backs off and whether `CFUN=4` parking could be
  added.

## Notes

- `Docs/_Admin/NB-IOT/index.md` links `NB-IOTT_Troubleshooting.md`, but the file is
  `NB-IOT_Troubleshooting.md` — broken link. `NB-IOT_RSSI_Estimation.md` and
  `DDS75_LB_Onboarding.md` are not listed there at all.

---

## Addendum 2026-09-12 — measurement data from the 2026-08-31 event

First real data on this problem. Ten readings around the most recent outage, with RSSI
*(verified — from the platform's own measurements)*:

```
2026-08-31 11:29:07  -89     2026-09-09 03:36:39  -89
2026-08-31 13:29:11  -87     2026-09-09 05:36:45  -89
2026-08-31 15:29:15  -91     2026-09-09 07:36:49  -87
2026-08-31 17:29:19  -87     2026-09-09 09:36:53  -93
2026-08-31 19:29:23  -87     2026-09-09 11:36:57  -89
```

### What the timing says

*(verified by arithmetic on the timestamps above)*

- **Uplink cadence is 7204 s** — 2 h plus 4 s of crystal drift per cycle — and it is
  *identical before and after the gap* (7204, 7204, 7204, 7204 → gap → 7206, 7204, 7204,
  7204).
- **Gap = 720 436 s = 8 d 8 h 07 m 16 s = 200.12 h.**
- **100 × 7204 s = 720 400 s.** The gap is 100 cadence periods to within 36 s — about
  0.36 s per cycle, inside the jitter already visible in the good data.

So the node ran **exactly 100 wake-up cycles**, woke ~99 times during the gap, and
delivered nothing on any of them. Its schedule was never disturbed: no reboot, no RTC
reset, no watchdog. The hardware was alive and on time the entire week.

- **RSSI is unchanged across the gap** (-87…-93 on both sides; the last successful uplink
  was a healthy -87 with no run-in degradation). This is not coverage, not the antenna, not
  a cell that went away. The radio was fine on both sides of a silence that started
  abruptly.

### This kills the clean wall-clock T3396 story

Recovery happens at the first scheduled wake-up after the block lifts, so the true block
duration is bounded by the two wake-ups either side:

| Assumed timer start | True duration must be in |
| ------------------- | ------------------------ |
| first failed cycle (2026-08-31 21:29:27) | (196.12, 198.12] h |
| last good uplink (2026-08-31 19:29:23) | (198.12, 200.12] h |

Legal GPRS Timer 3 values near there are only **190 h** and **200 h** (10-hour unit; the
1-hour unit caps at 31 h, the 10-minute unit at 5.17 h, the next unit up is 320 h).
**Nothing lands in the first window.** 200 h fits the second window, but only at its exact
top edge, and only if the reject arrived in the same session as the successful uplink.
That is a lot of special pleading for one coincidence.

### Bram is probably right, and the mechanism is forced by the hardware

His model — short back-off, stretched by the duty cycle — fits without special pleading.
If the timer only advances while the modem is powered, then

> back-off value = 100 cycles × modem-on time per cycle

| Back-off | Implied modem-on time per cycle |
| -------- | ------------------------------- |
| 30 min | 18 s |
| 1 h | 36 s |
| 2 h | 72 s |
| **3.1 h (T3346 maximum)** | **112 s** |
| 10 h | 360 s |

Every one of those is a plausible modem-on window for a battery node. And there is a
physical reason to expect exactly this behaviour: **if the Dragino removes VBAT from the
BC660K between uplinks, the module has no running clock while it is off.** It therefore
*cannot* compute how much wall-clock time elapsed. The only thing it can do at power-up is
reload the residual from NVRAM and carry on counting. 3GPP says "continue counting"; with
the power cut, the only available implementation of that is "resume the residual", which
silently converts a wall-clock back-off into one measured in *modem-powered seconds*.

**This reverses the conclusion in finding #1 above.** That finding ruled out T3346 because
its 186-minute ceiling cannot span a week. That reasoning only holds for a timer running as
wall clock. Under the duty-cycle mechanism, **T3346 at or near its 186-minute maximum is now
the leading candidate** — 186 min / 100 cycles = 112 s of modem-on per cycle, the most
plausible number in the table.

That also rehabilitates Bram's original pointer completely: `AT+QEMMTIMER?` *does* expose
T3346 as `timerID 0`, with `<remain_time_value>` in seconds. On a stuck node it should read
something like `+QEMMTIMER: 0,0,<seconds>` with the remaining value well under 11 160 s even
after days of silence — which would be the whole proof in one line.

### The two decisive tests

**1. Duration versus uplink interval — from data we already hold, no hardware needed.**
The two models predict different things:

| Model | Prediction |
| ----- | ---------- |
| Wall-clock timer | Outage length is roughly constant, whatever the node's TDC |
| Duty-cycle (Bram) | Outage length scales with TDC — halve the interval, halve the outage |

So: for each of the ~5 historical events, pull the gap length *and* that node's uplink
interval. A node on a 1 h TDC that was out ~4 days next to this 2 h node out ~8 days
settles it immediately. If every affected node was on 2 h, the test is uninformative and we
need test 2. Note the user recalls these as "around 7 days" — this one is 8.3 days, so the
durations are not all identical, and their spread is itself evidence.

**2. Residual-survives-power-cycle test, on a stuck node over UART.**
Read `AT+QEMMTIMER?` and note the remaining seconds. Power the module down for 30 minutes.
Power up and read again. If the residual dropped by ~0 s instead of ~1800 s, the timer does
not run while off, and Bram's mechanism is confirmed outright.

### What this changes about the fix

If the duty-cycle mechanism holds, the remedy is small and entirely in our control, and it
is what Bram said: **keep the modem powered until the timer expires** rather than cutting
power. One uninterrupted `CFUN=4` (or plain powered-idle) stretch of ~3 hours clears a
T3346 at maximum. That is 3 hours instead of 8 days.

Two field remedies follow that need no firmware change from Dragino:

- **On-site rescue:** connect UART, read the residual with `AT+QEMMTIMER?`, leave the module
  powered until it expires, then `AT+CFUN=1`. Minutes to hours, not days.
- **Shorten the TDC while blocked:** under this model the back-off burns down in proportion
  to powered time, so a node temporarily on a 10-minute TDC clears roughly 12× faster than
  one on 2 hours. Costs battery, but only for the duration of the block.

The durable fix is a firmware request to Dragino: on an attach/PDN reject, read the back-off
residual and either hold the modem in `CFUN=4` until it expires, or at minimum stop
re-attempting every cycle. Worth sending them this note's capture once we have one.

### Revised open questions

- Does the Dragino firmware for this model **cut VBAT** to the BC660K between uplinks, or
  park it in PSM? PSM keeps the module's clock running and would break the mechanism above.
  This is the single most load-bearing unknown and it is answerable from the model's manual.
- Which model was the node in this event, and what were the TDCs of the other ~4 events?
- Superseded: "were the durations all ≈170 h?" — this one was 200.12 h, so no. The new
  question is whether duration correlates with TDC rather than clustering on one value.

### Addendum 2, same day — `AT+CSQTIME` pins the modem-on window

The node in the 2026-08-31 event is an **LDDS75-NB** (Dragino's NB-IoT DDS75 / NDDS75).
The same symptom has been seen on a **PS-NB** (Bram's own sensor). Same BC660K-GL, same
firmware family, different product — so this is not a one-model defect.

The estimate in Addendum 1 assumed a short modem-on window (~112 s) and concluded T3346.
**That was wrong**, and the correction is in our own onboarding doc:

```
# Extend network acquisition from 5 to 10 minutes
AT+CSQTIME=10
```
— `Docs/_Admin/NB-IOT/NDDS75_Onboarding.md`

*(verified — Dragino's general -NB/-NS manual: "By default, device will search network for
5 minutes. User can set the time to 10 minutes by AT+CSQTIME=10 so it can search longer."
The factory default shows as `"AT+CSQTIME":"5"` in that manual's config dump.)*

So on a cycle where the node cannot get onto the network it keeps the modem powered and
searching for **up to 10 minutes**, not ~2. Redoing the duty-cycle arithmetic over the 100
observed cycles:

| Candidate back-off | Required modem-on per blocked cycle |
| ------------------ | ----------------------------------- |
| T3346 at maximum (186 min) | 1.9 min |
| T3396 = 10 h (1 × 10 h) | 6.0 min |
| T3396 = 15 h | 9.0 min |
| **T3396 = 17 h (17 × 1 h)** | **10.2 min** |
| T3396 = 20 h (2 × 10 h or 20 × 1 h) | 12.0 min |

**T3346 is excluded again.** At CSQTIME-length cycles its 186-minute maximum burns down in
~19 cycles — 37 h, 1.5 days — nowhere near the observed 8.34 days. Addendum 1's "T3346 is
now the leading candidate" only held under the 112 s assumption and does not survive.

**T3396 ≈ 17 h is the best fit**, and it is a striking one: 10.2 min of modem-on per blocked
cycle is exactly CSQTIME=10 plus a few seconds of boot and teardown. Forward-predicting from
the settings rather than fitting to them:

> 17 h ÷ 10 min = 102 cycles × 2 h TDC = **8.50 days predicted**, against **8.34 days
> observed**.

So the working hypothesis is now **both halves together**: an ordinary ESM back-off of
~17–20 h (Bram's "short period"), stretched ~12× by the duty cycle (Bram's mechanism)
because the timer only advances while the modem is powered.

#### Caveat that the battery trace settles

This assumes a blocked cycle really does consume the full CSQTIME. That holds if the block
is at attach level. If T3396 blocks only the PDN connection while the attach itself
succeeds, the node registers in seconds, fails the send, and powers down early — a much
shorter `t_on` and therefore a much smaller timer (a 1 min window implies only ~1.7 h).

**This is measurable from data we already hold.** The nodes report battery voltage, so:
plot `BatV` for this LDDS75-NB across 2026-08-31 → 2026-09-09 and compare the slope during
the gap with the slope either side. A blocked cycle at 10 min of powered radio costs roughly
20× a normal cycle, which over 100 cycles is unmistakable.

- Steep drain through the gap → `t_on ≈ 10 min` → back-off ≈ 17–20 h, ESM/T3396.
- Near-normal drain → the node fails fast, `t_on` is seconds-to-a-minute → a much smaller
  back-off, and the multiplier is correspondingly larger.

Either branch confirms the mechanism; they differ only on the timer value, and the fix is
the same for both.

#### `AT+CSQTIME=10` has been quietly halving these outages

An unintended consequence worth knowing. The same 17 h timer, at different settings:

| CSQTIME | TDC 1 h | TDC 2 h | TDC 6 h |
| ------- | ------- | ------- | ------- |
| 5 min (factory default) | 8.5 d | **17.0 d** | 51 d |
| 10 min (our setting) | 4.3 d | **8.5 d** | 25.5 d |
| 20 min | 2.1 d | 4.3 d | 12.8 d |

Outage length is proportional to `TDC ÷ CSQTIME`. Raising CSQTIME shortens these blackouts
in direct proportion — but it is the same knob that drains the battery on genuinely weak
sites, which is exactly the failure mode we warn customers about in
`Docs/Sensor_Overzicht.md`. Don't raise it fleet-wide on this reasoning alone; it is a
rescue lever for a node known to be blocked, not a default.

#### Cheapest confirmation available: ask Bram

His PS-NB had the same symptom. If his TDC or CSQTIME differ from ours, his outage duration
should differ **in proportion to `TDC ÷ CSQTIME`** — that single comparison tests the
mechanism with no hardware, no waiting, and no 1NCE ticket. Ask him for: node TDC, CSQTIME,
and the exact first-missing/first-returning timestamps of his outage.

#### Still unknown

- Whether the firmware cuts VBAT to the BC660K or parks it in PSM. Still the load-bearing
  assumption; the per-model manuals checked so far do not state it. A current measurement
  across one sleep interval would answer it directly (PSM ≈ 3–5 µA with the module's clock
  alive; fully off also ≈ µA but with no clock — so the residual-survives-power-cycle test
  in Addendum 1 is the better discriminator).
- Whether a blocked cycle consumes the full CSQTIME (see battery trace above).

---

## Addendum 3, 2026-09-25 — fleet-wide gap analysis against production InfluxDB

A live case was reported (`F867787054209469`), so the fleet query proposed in Addendum 1
was finally run: 200 days of `waterlevel` for all 41 devices, read-only against production
Influx. *(verified — all figures below computed from that extract.)*

SIM identifiers for the live node (IMEI/IMSI/MSISDN/ICCID) are deliberately **not** recorded
here; they are in the 1NCE portal and the admin sheet. The platform DevEUI is enough.

### The 2026-08-31 event belongs to F867787054437045

The timestamps in the original report were local time (UTC+2). In UTC the gap is
`2026-08-31 17:29:23Z → 2026-09-09 01:36:39Z`, 200.12 h, 100.0 cycles — identical to the
arithmetic in Addendum 1, now with the device identified.

### Multi-day events are far more common than we thought

Not "≈5 times in 3 years". In the last ~6 months alone, across 5 different devices:

| Device | Gap start (UTC) | Duration | Cycles | RSSI before/after |
| ------ | --------------- | -------- | ------ | ----------------- |
| F867787054436393 | 2026-04-08 19:14 | 226.1 h (9.42 d) | 113 | −102 / −107 |
| F860631074843179 | 2026-07-15 22:39 | 166.3 h (6.93 d) | 83 | −87 / −93 |
| F867787054545078 | 2026-08-23 12:57 | 244.5 h (10.19 d) | 122 | −89 / −89 |
| F867787054437045 | 2026-08-31 17:29 | 200.1 h (8.34 d) | 100 | −88 / −89 |
| F867787054545078 | 2026-09-12 16:29 | 196.0 h (8.17 d) | 98 | −84 / −91 |

The 2026-08-23 row is suspect: it follows an 873 h gap across which batV went 2.142 → 3.284,
i.e. the device was serviced or re-batteried, so that silence may be physical, not network.

**Two devices are in an outage right now**, not one:

- `F867787054209469` — last reading 2026-09-18 16:56:36Z, silent **169.5 h** (7.06 d, 84.6
  cycles). This is the reported one.
- `F867787054545078` — last reading 2026-09-20 20:27:47Z, silent **118.0 h** (4.92 d).
  This one had not been noticed.

Durations so far span 166–226 h. **No single legal GPRS Timer 3 value fits them as
wall-clock**, and none fits as a constant under the duty-cycle model either. That is not
evidence against back-off: 3GPP has the network pick the value *randomly within a configured
range*, so a spread is expected and we should stop hunting for one constant.

### RSSI still exonerates coverage — more strongly now

Every multi-day gap shows unchanged RSSI across it, and the affected devices span the whole
signal range in the fleet (median RSSI −85 to −105). Meanwhile the two genuinely
signal-starved nodes (`F867787054434570`, `F867787054436575`, both pinned at −111) show a
completely different signature: dozens of *short* 12–28 h gaps, then permanent death in late
June. Bad coverage looks nothing like this.

### The battery test proposed in Addendum 2 does not work — abandon it

*(verified, and negative.)* Li-SOCl₂ sits on a flat plateau and the reported `batV` is
noisy: median step between consecutive readings 0.0020 V, with excursions to 0.056 V. The
predicted difference between the two models was 0.002–0.08 V *total* across a whole gap.
Computing it anyway on four events gave actual/predicted ratios of −1.94, 0.10, 2.05, −1.26
— pure noise. The idea was sound but this hardware cannot report charge finely enough.
**Do not spend more time on it.**

### The real find: a 12.02 h gap class, 62 instances across 11 devices

The same shape in miniature, and it is startlingly regular:

- **62 occurrences**, on **11 of the 12 active NB devices**
- median duration **12.020 h**, minimum 12.015 h
- in cycles: **6.000** at the low end (a minority stretch to 6.43)

Six missed cycles, to the second, over and over, on nearly every node, with RSSI and battery
unchanged either side. Nothing about radio conditions is that quantised. This is the same
phenomenon as the multi-day outages, two orders of magnitude more frequent.

It fits **both** models exactly, which is why it is so useful:

| Model | Value implied by a 12.02 h / 6-cycle gap |
| ----- | ---------------------------------------- |
| Wall-clock | T3396 = **12 h** — a standard, extremely common operator value, with the timer started at the *last successful* uplink |
| Duty-cycle (Bram) | T3396 = **1 h**, stretched by 6 × CSQTIME(10 min) |

Both are legal, both are common, and the two predictions are numerically identical here. The
fleet data cannot separate them. **But it no longer has to**: this event reproduces every few
weeks per node instead of twice a year. `F867787054436476` has logged 15 of them,
`F867787054437045` a dozen. Attach a UART logger to one of those and the answer arrives in
days, not months — and it is the *same* question, because if the 12 h class is a back-off
then so are the 200 h ones.

### Platform-side: timestamps look receipt-based, not record-based

Worth knowing, and a separate thread. `F867787054545078` shows two rows at the *identical*
timestamp `2026-09-20 20:27:47Z` with different batV and RSSI, and on 2026-09-12 six rows
minutes apart (15:34, 15:52, 16:10, 16:12, 16:25, 16:29) instead of on the 2 h cadence.

That is the `AT+NOUD=8` buffer flushing several stored records in one uplink, with the
platform stamping them all at arrival rather than using each record's own time. Consequences:
gaps in Influx measure *delivery*, not capture — the node may well have sampled through the
outage and delivered later — and buffered history is being collapsed instead of backfilled.
The cycle arithmetic in this note is unaffected (it is about delivery), but the storage
behaviour deserves its own investigation.

### Config nit found on the way

`Influx/Endpoint` is `https://grafana.foxinnovations.be:58086/`, but the certificate on that
host (Let's Encrypt, renewed 2026-09-20 19:16 UTC) carries only `influxdb.foxinnovations.be`
— `grafana.…` is not in the SAN list, so strict TLS validation fails there. Ingest is
demonstrably unaffected (devices wrote normally through 2026-09-25), so this is not the cause
of anything here, but the hostname in config should be moved to `influxdb.foxinnovations.be`
before something does start validating.

### Revised next actions

1. **Go to `F867787054209469` while it is still blocked** and run the Addendum 1 capture over
   UART — `AT+QESMC?` and `AT+QEMMTIMER?` above all. Live evidence, available today, and on
   past durations it may recover within ~0–2.5 days.
2. **Ask 1NCE for data *usage* per day for that ICCID since 2026-09-18**, not connection
   status. The portal showing "Online" while the node has delivered nothing for 7 days means
   the status field reflects a stale or session-level attribute, not reachability — zero
   usage is the number that would actually corroborate a block.
3. **Instrument a 12 h repeater** (`F867787054436476` or `F867787054437045`) with a UART
   logger and let it catch one. This is now the primary experiment; the 8-day events are too
   rare to wait for and ask the same question.
4. Superseded: the battery-slope test (Addendum 2) and the TDC-versus-duration test
   (Addendum 2) — every NB node in the fleet runs TDC = 2 h, so there is no variation to
   exploit. Deliberately setting one node to TDC = 1 h would restore that test.

---

## Addendum 4, 2026-09-25 — "attached" in 1NCE puts the fault at the ESM layer

*(reported by the operator's portal, not measured by us.)* For `F867787054545078`, also
confirmed blocked, the 1NCE portal shows the SIM as **attached** — where the earlier check on
`F867787054209469` showed "Online".

This is the single most useful fact so far, because attach and session are different layers
and the back-off timers live on different ones:

| Layer | State per 1NCE | Timer that gates it |
| ----- | -------------- | ------------------- |
| EMM / mobility — attach, TAU | **working** (SIM is attached) | T3346 |
| ESM / session — PDN connectivity | **not delivering data** | T3396 |

An attached SIM that passes no data is the textbook signature of an **ESM back-off**. It
rules T3346 out directly — no need for the residual test to settle that part — and it rules
out coverage a second time: the radio link is up, the node is on the network, the cell is
fine. What it cannot do is establish or use a PDN session.

Three consequences:

1. **`AT+QESMC?` is now the command that matters**, not `AT+QEMMTIMER?`. The EMM timer query
   will show nothing, exactly as predicted in finding #3, and that absence will look like
   "no back-off" if it is the only command run. Run `AT+QESMC?` first.
2. **`AT+CSQTIME` is probably irrelevant to these events.** CSQTIME governs network *search*.
   A node that attaches successfully never enters that wait — it attaches in seconds, fails
   the session, and powers down. So the modem-on time on a blocked cycle is likely tens of
   seconds, not 10 minutes, and **the whole duty-cycle multiplier computed in Addendum 2 is
   built on the wrong number**. At a short `t_on`, Bram's model implies a back-off of well
   under 2 h for the 200 h event, and the 12.02 h class becomes a ~6-minute timer. Neither is
   impossible, but the clean wall-clock reading of the 12 h class (T3396 = 12 h, a standard
   operator value) now looks considerably more likely than the duty-cycle one.
3. **The question for 1NCE is now specific.** Not "is the SIM online" but: *the SIM is
   attached and has passed zero data since <date> — what is rejecting the PDN connectivity
   request, and with which ESM cause?* They can see this; it is the same number `AT+QESMC?`
   would return.

### The multi-day durations still fit no standard timer value

With the true value bounded by the wake either side, the four events need a legal GPRS
Timer 3 value in (164.3, 166.3], (194.0, 196.0], (198.1, 200.1] and (224.1, 226.1] hours.
The 10-hour unit offers 170, 200, 200, 230 — only the third fits. Nor do they divide into a
chain of 12.02 h blocks (83, 98, 100, 113 cycles ÷ 6 = 13.8, 16.3, 16.7, 18.8 — no
integers). So the multi-day outages are probably **not one signalled back-off**, and may be a
compounded or operator-side condition distinct from the clean 12 h class.

### Outages cluster across devices, and the LoRaWAN fleet is clean

*(verified from the same extract.)* Genuine multi-day NB outages, discounting the 873 h and
1474 h intervals that are servicing/dead periods:

```
F867787054545078  09-12 16:29Z -> 09-20 20:27Z
F867787054209469  09-18 16:56Z -> ongoing          overlap 51.5 h
F867787054545078  09-20 20:27Z -> ongoing          overlap 118.4 h
F867787054437045  08-31 17:29Z -> 09-09 01:36Z
```

Three devices in overlapping multi-day outages inside one month. Meanwhile the LoRaWAN half
of the fleet — a completely separate path, TTN rather than cellular — shows **one** gap over
6 h across ~25 devices since 2026-09-01. So this is not the platform, not ingest, and not
anything shared downstream of the two radio paths. It is specific to the cellular side, and
it hits several SIMs in the same window.

### `F867787054545078` recovered for exactly one uplink

Its 196 h outage ended at 2026-09-20 20:27:47Z with a burst of **two buffered records at the
identical timestamp** — and then nothing for the 118 h since. One successful session in
thirteen days. Under an ESM back-off that is what re-arming looks like: the timer expires,
the node gets one PDN session through, and the next request is refused again. It is also the
cleanest argument that the block is being re-applied by the network rather than simply
running down.

---

## Addendum 5, 2026-09-25 — live experiment: 1NCE "Reset connection"

At **≈2026-09-25 18:54Z** the 1NCE portal's *Reset connection* action was triggered on both
currently blocked SIMs (`F867787054209469`, `F867787054545078`). Recorded here as t0 because
the timing is what makes it informative.

Expected wake times, from each node's own cadence (7204 s, held steady through every previous
outage):

| Device | Silent since | Next wake after t0 | Then |
| ------ | ------------ | ------------------ | ---- |
| F867787054209469 | 09-18 16:56:36Z (170.0 h) | **19:02:16Z** (t0 + 8 min) | 21:02:20Z, 23:02:24Z |
| F867787054545078 | 09-20 20:27:47Z (118.4 h) | **20:31:47Z** (t0 + 97 min) | 22:31:51Z, 00:31:55Z |

A poller is watching Influx for anything newer than those last deliveries.

### What each outcome means

The reset acts on the **network** side — it clears the operator's session state for the SIM.
It cannot reach into the module's NVRAM. So:

- **Return at the very next wake** (19:02:16Z, eight minutes after the click) — near-certain
  causation given the resolution. The block was operator-side session state, and **not** a
  UE-held back-off timer. That would overturn the central hypothesis of this whole note.
- **Return several cycles later** — ambiguous; could be natural expiry, since 209469 is at
  170 h and past events ran 166–226 h.
- **No return within a few cycles** — the reset was inert, consistent with a timer held in
  the UE that only the UE can retire.

*Caveat, marked as uncertain:* a network-initiated detach may in some implementations
legitimately cause the UE to drop a running back-off, so a fast return does not by itself
prove the timer was never there. The clean inference runs the other way — inertness is
strong evidence *for* a UE-held timer.

### Cost of the experiment

Resetting destroys the evidence we had been waiting months to collect. Both blocked nodes
were live subjects for the `AT+QESMC?` capture, and after a successful reset there is nothing
left to read. This was the right call with customers missing data, but it means the UART
capture should be taken **before** the reset next time — the portal action is one click and
will still be there ten minutes later.

### If it works, that is the operational answer

A one-click remedy that converts an 8-day outage into a ~2-hour one is worth more day to day
than knowing the exact ESM cause. It would slot straight into the `Data` alarm path: alarm
fires at 24.5 h → check 1NCE → reset connection. That belongs in `runbooks/` once it has
worked twice.

### Addendum 5 result, 2026-09-25 19:46Z — the reset was inert

*(verified from production Influx.)*

`F867787054209469`'s first scheduled wake after the reset, **19:02:16Z**, produced nothing.
At 19:45:59Z both nodes are still dark: 170.8 h and 119.3 h silent.

**Ingest is proven healthy at the same moment** — the control that matters:

| Device | Last delivery | Age at 19:45:59Z |
| ------ | ------------- | ---------------- |
| F867787054436476 (NB) | 19:44:40Z | **1 min** |
| F860631071942560 (NB) | 18:44:34Z | 1.0 h |
| F867787054436393 (NB) | 18:17:36Z | 1.5 h |
| F867787054437045 (NB) | 17:52:13Z | 1.9 h |
| ~20 LoRaWAN devices | all within 3.5 h | — |
| F867787054545078 | 09-20 20:27Z | 119.3 h |
| F867787054209469 | 09-18 16:56Z | 170.8 h |

Another NB node delivered through the same UDP server one minute before the check. Server,
listener, ingest and Influx are all fine; the failure is specific to these two SIMs.

Per the decision table in Addendum 5, inertness is the **strong** direction: a network-side
reset cannot reach the module's NVRAM, so a UE-held back-off survives it. One clean wake
cycle with no delivery is consistent with that. Not yet conclusive — it is one cycle.

**Unresolved contradiction:** 1NCE reported the SIM as "connected" after the reset, which
implies a PDN connectivity request was accepted — something a UE sitting on an unexpired
T3396 would not send. Either the portal status is an artifact of the reset action rather than
device behaviour, or the device did get a session and its UDP payload is being lost
downstream of the operator. These point opposite ways and the question is still open.
Per-SIM usage is only visible monthly in the portal, so this needs either the NB-IoT UDP
receiver's own log or 1NCE's API (`GET /management-api/v1/sims/{iccid}/events`).

Next wakes: `F867787054545078` 20:31:47Z, `F867787054209469` 21:02:20Z.

### Addendum 6, 2026-09-25 — server-side proof: the devices are not transmitting at all

*(verified on server3, `/var/log/syslog`, service `IotWebhookBridge.UdpServerService`.)*

The NB-IoT receiver logs every datagram as `Udp packet received from <ip>, DataLength <n>`.
It records the **source IP, not the device**, and NB traffic arrives from 1NCE's AWS
eu-central-1 egress addresses (18.196.220.3, 3.125.204.250, 18.195.39.164, 18.198.73.229),
so individual devices cannot be told apart by IP. Payload sizes separate the models:
**69, 78 and 99 bytes** are the sensor uplinks; the 21-byte packets every 5 minutes from
213.118.73.11 are an unrelated fixed-line heartbeat.

**At `F867787054209469`'s expected wake — 21:02:16 local / 19:02:16Z — no sensor packet
arrived.** The only traffic in the 21:00–21:10 window was two 21-byte heartbeats.

Daily counts settle it beyond that single wake:

| Date | len=69 | len=78 | len=99 |
| ---- | ------ | ------ | ------ |
| 09-16 | 50 | 12 | 12 |
| 09-17 | 50 | 12 | 12 |
| 09-18 | 50 | 12 | 11 | ← 209469 goes silent 16:56Z |
| 09-19 | 42 | 12 | 12 |
| 09-20 | **37** | 12 | 12 |
| 09-21 | 37 | 12 | 12 |
| 09-22 | 38 | 12 | 11 |
| 09-25 | 40 | 11 | 10 |

The 69-byte stream steps from ~50/day to ~37/day when `209469` blocks — a drop of ~13,
against the 12 uplinks/day a 2 h TDC produces. One device's worth, exactly. The 78- and
99-byte streams (other models) are untouched at 12/day throughout.

**Conclusion: no packets are being dropped, parsed wrongly, or lost. The blocked devices
send nothing at all.** Every datagram that reaches the server is accounted for in Influx.

This resolves the Addendum 5 contradiction: the 1NCE "connected" status does **not** reflect
device transmission. It is an artifact of the reset action on the operator's own session
record. The device never sent a byte.

### Correction to Addendum 5: the evidence was not destroyed

Addendum 5 warned that resetting would burn the live subjects. It did not — the reset was
inert, and **both nodes are still blocked and still available for the UART capture.** That
capture is now the highest-value action available and the window is open, not closed.

What the block is *not*, after today: not coverage (RSSI normal either side, and the SIM
attaches), not the platform, not ingest, not the UDP path, not parsing, and not
operator-side session state that a reset can clear. The MCU is also demonstrably alive
through these outages — the 200 h event resumed exactly on schedule at cycle 100, so the
node's timer never stopped. Whatever refuses is between the MCU and the air interface.

### Addendum 7, 2026-09-25 21:03Z — F867787054209469 recovered on its own

*(verified from production Influx and the server3 UDP log.)*

```
2026-09-25T21:03:19.528562Z   batV=3.304  RSSI=-97   dist=2179
2026-09-18T16:56:36.000000Z   batV=3.320  RSSI=-103  dist=1490   <- last before the gap
```

- Outage: **172.112 h** (7.171 d) = **86.01 cycles**
- Delivered **59 s** after its scheduled 21:02:20Z wake — normal attach-and-send latency
- RSSI −97, in line with its usual −99…−103. Nothing changed about the radio.

**The reset did not do this.** The click was at 18:54:21Z; the very next wake, 19:02:16Z,
produced no packet at all (confirmed in the server log, not merely absent from Influx).
Recovery came only at the *second* wake, 2.15 h later. A network-side unblock would have
taken effect at the first wake. And 172.11 h falls inside the 166–226 h band of every
previous event. Natural expiry is by far the more parsimonious reading: **the reset was
inert, and "Reset connection" should not be treated as a remedy.**

#### One tempting arithmetic fit, probably a coincidence

Recovery lands at the first wake after expiry, so under the rule *timer starts at the first
failed cycle*, the true value is bounded by (168.09, 170.09] h — and **170 h = 17 × 10 h is a
legal GPRS Timer 3 value** sitting right in it. Neat. But applying the identical rule to the
other four events:

| Event | Duration | T must be in | Legal 10 h values there |
| ----- | -------- | ------------ | ----------------------- |
| F867787054209469 | 172.11 h | (168.09, 170.09] | **170** |
| F860631074843179 | 166.34 h | (162.34, 164.34] | none |
| F867787054545078 | 195.97 h | (191.97, 193.97] | none |
| F867787054437045 | 200.12 h | (196.12, 198.12] | none |
| F867787054436393 | 226.12 h | (222.12, 224.12] | none |

One hit out of five is what coincidence looks like. Recorded so nobody re-derives it and
mistakes it for a result.

#### Customer-visible consequence

`dist` went 1490 → 2179 mm across the gap: the well dropped 689 mm and the platform showed a
flat line through all of it. That is the actual cost of these outages.

#### Only F867787054545078 is still blocked

At 21:03Z it is **120.6 h** silent (since 2026-09-20 20:27:47Z). On the 166–226 h range it
has roughly 45–105 h left. It is now the *only* candidate for tomorrow's collection, which
settles the choice made in the runbook.

### Tooling added

`scripts/nbiot/capture.py` — read-only diagnostic sweep over UART (9600 8N1, pyserial),
every response stamped in UTC and logged. Runs the runbook's command list with per-command
timeouts from the BC660K manual's "Maximum Response Time" column. Sends nothing that changes
state.

`scripts/nbiot/poll.py` — the continuous-power experiment: holds `CFUN=1` and polls
`AT+CGATT?` / `AT+CGACT?` / `AT+QESMC?` every minute, flagging the moment a PDP context comes
up and reporting how long that took. Compare against 166.3 / 172.1 / 196.0 / 200.1 / 226.1 h.

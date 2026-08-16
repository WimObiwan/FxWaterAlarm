# 2026-08-16 — Quote e-mail to a prospect (anonymised)

## Problem

A prospect asked for a quote for a single rainwater cistern at a fairly remote location
where WiFi/ethernet near the pit is not available. The reply had to cover: which sensor,
which connectivity, the total price, and what happens if it does not work on site.

This file records the commercial terms that were quoted, because several of them are **not
written down anywhere else** — not in `Docs/`, not in the code. Prices themselves live in
[`../../Docs/Prijzen.md`](../../Docs/Prijzen.md); only the terms that page does not cover
are recorded here.

Anonymised: no name, no address, no contact details. The prospect is "the customer".

## What was quoted

**Situation.** One pit, remote location, no WiFi/ethernet available near the pit, so LoRa
with a local gateway was ruled out by the customer. Address sits in a green zone on the
mobile-coverage map, but at the *edge* of it.

**Recommendation given.**

* Pressure sensor ("dieptesensor") rather than ultrasonic, to remove the measurement risk
  (false echoes, high manhole) — see `Docs/Sensor_Overzicht.md#wat-kan-er-misgaan`.
* NB-IoT ("5G") rather than LoRa, since WiFi/ethernet for a gateway is unavailable.
* Residual risk stated openly to the customer: NB-IoT reception at the edge of the coverage
  zone is not guaranteed (manhole cover, buildings). Removing that risk requires
  WiFi/ethernet + LoRa + gateway.

**Prices quoted** (all excl. VAT): pressure 5G 220 €, pressure LoRa 200 €, ultrasonic 5G
160 €, ultrasonic LoRa 140 €, gateway 130 €, subscription 30 €/year from year two.

> ⚠️ The two pressure-sensor prices **contradict `Docs/Prijzen.md`**, which lists 240 €
> (5G) and 220 € (LoRa) — the site charges ultrasonic + 80 €, this quote charged
> ultrasonic + 60 €. Unresolved at the time of writing; see open questions.

## Terms not documented in `Docs/`

| Term | What was quoted |
| ---- | --------------- |
| Installation by us | 100 € for a straightforward situation; default assumption is that the customer installs it themselves |
| Shipping | bpost rates, checked at quote time: ~5,40 € to a pickup point, ~7,10 € to an address |
| Return if installation fails | Everything taken back as long as it is undamaged; full refund **except** shipping both ways |
| Return after > 1 month | 4 €/month deducted for battery and SIM usage |
| Installation effort | 2 screws for the sensor module, 2 screws for the cable holder (holder supplied) |

**Track record, as told to the customer:** a return has never been needed so far. Upgrades
have happened — 5G → LoRa, and ultrasonic → pressure sensor. That matches the failure modes
already documented in `Docs/Sensor_Overzicht.md`.

## Findings

* **The return policy is the actual sales instrument here**, not the price. The customer's
  hesitation was reception risk, and the answer that lands is "if it does not work, you are
  not stuck with it".
* **An upgrade beats a return.** Since 5G → LoRa upgrades have happened and returns have
  not, offering the upgrade path (pay only the difference) is a stronger answer than
  offering the refund, and cheaper for us — the customer keeps a working installation.
* **The return policy has no time limit as quoted.** "4 €/month after one month" runs
  indefinitely; nothing caps how late a return can be requested.
* **The battery promise in the mail is broader than the site's.** The mail says the
  subscription includes "SIM and any battery replacement"; `Docs/Prijzen.md` says one
  battery per sensor per year, **shipping excluded**.
* **The coverage map was attributed to "IBBT"** in the mail. IBBT merged into imec in 2012
  and imec does not publish coverage maps; the Belgian regulator BIPT does. Source not
  verified at the time of writing — reported, unverified.
* Provenance for everything above: read from the outgoing e-mail draft, cross-checked
  against `Docs/Prijzen.md` and `Docs/Sensor_Overzicht.md` as they stood on 2026-08-16.

## Addendum 2026-08-16 — pressure-sensor price resolved

The **site prices are the correct ones**: pressure sensor 220 € (LoRa) / 240 € (5G), i.e.
ultrasonic + 80 €. Confirmed by Wim. The 200/220 € figures in the draft quote were stale
and were corrected before the mail went out; `Docs/Prijzen.md` needs no change.

Consequence for the quote: the recommended configuration (pressure sensor on 5G) is
**240 €**, not 220 €, and the LoRa alternative is 220 € + 130 € gateway = 350 €.

Open question 1 below is answered by this addendum.

## Addendum 2026-08-16 — terms published, installation fee restructured

The terms that this file recorded as "not documented in `Docs/`" have been **published in
[`../../Docs/Prijzen.md`](../../Docs/Prijzen.md)**: new sections *Installatie* and
*Verzending*, and *Omruilen* / *Terugname* under "Wat als een sensor toch niet past?".
Quotes should now link to that page instead of restating the terms from memory.

The **installation fee changed** in the process. It is no longer a flat 100 €; it is
distance-based from Torhout:

| Distance | Price |
| -------- | ----- |
| < 30 km | 50 € |
| < 100 km | 100 € |
| further | on request |

Per trip, not per sensor — several sensors at one location cost the same. The quote in this
file predates that change and used the flat 100 €.

The **return policy was tightened** at the same time: returns are accepted **up to six
months** after delivery, and the monthly deduction for battery and SIM usage went from 4 €
to **5 €/month** (still starting one month after delivery). The quote in this file predates
both changes.

For this one prospect the **flat 100 € installation fee was honoured** even though the
address is ~110 km from Torhout and therefore falls in the "on request" band. Treat that as
a one-off, made because the quote had already gone out at that price — not as a precedent
for the > 100 km band.

Open questions 2, 3 and 4 below are answered by this addendum.

## Open questions

1. ~~**Which pressure-sensor price is correct** — 220/240 € (site) or 200/220 € (this
   quote)?~~ Answered: the site is correct, see the addendum above.
2. ~~Should the installation fee (100 €), the shipping rates and the return policy be
   published in `Docs/Prijzen.md`?~~ Answered: yes, published — see the addendum above.
3. ~~What is the intended **time limit** on returns?~~ Answered: six months after delivery,
   at 5 €/month from month one — see the addendum above.
4. ~~Does the subscription cover battery *shipping* or not?~~ Answered: it does **not** —
   shipping of a replacement battery is charged separately, now stated on the price page.
5. VAT: quotes are stated excl. VAT, which is the wrong frame for a private customer.
   Should quotes to consumers show incl. VAT?

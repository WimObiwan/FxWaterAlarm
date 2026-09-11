# 2026-09-09 — Edits Wim made to a drafted quote e-mail

## Problem

A prospect (rainwater cistern, ~55 km from Torhout) asked for a quote. A draft reply was
generated from `Docs/Prijzen.md` + `Docs/Sensor_Overzicht.md`, then Wim edited it by hand
before sending. This file records **what he changed and what that implies**, so the next
generated quote starts closer to the finished article.

The durable rules extracted from this are in
[`../runbooks/write-prospect-quote.md`](../runbooks/write-prospect-quote.md). Earlier
thread on the same subject: [`2026-08-16-prospect-quote-email.md`](2026-08-16-prospect-quote-email.md).

Anonymised where it does not matter; the address is kept only where it explains an edit.

## What was checked

Word-level diff between the generated draft and the draft as it stood in Thunderbird's
`Concept` folder after Wim's edits (read back via the Thunderbird MCP, 2026-09-08 22:02).
Nine changes. Provenance for the list below: **read from the two drafts**. The *reasons*
are inferred unless marked otherwise.

## Findings

### Additions

| # | Edit | Reading |
| - | ---- | ------- |
| 1 | Added after the intro: *"Sowieso bel ik je één dezer dagen nog op om het kort te bespreken."* | A quote to a private customer is an opener for a phone call, not a self-service document. |
| 2 | Gateway bullet gained: *"De gateway kan wel gemakkelijk gedeeld worden met buren, tot enkele huizen ver."* | A selling point for LoRa that is **not** in `Docs/`: no WiFi of your own near the pit does not automatically rule LoRa out — a neighbour's gateway can cover it. |
| 3 | New paragraph naming the customer's address and stating NB-IoT coverage was checked on the **BIPT** map, with an explicit *"maar 100 % zekerheid is er hierover niet"* | Coverage is checked **per address, before quoting**, and the residual risk is stated in the mail. Same move as in the 2026-08-16 quote — this is habit, not a one-off. |
| 4 | Self-install paragraph gained: *"Bij zelf-installatie is de eventuele verzending voor terugname ten laste van u."* | Closes a gap: self-install shifts return shipping to the customer. |
| 5 | Battery bullet gained *"wanneer deze leeg is"* → *"een vervangbatterij, **wanneer deze leeg is**, tot één per sensor per jaar"* | Prevents reading the yearly battery as an automatic annual shipment. |

### Removals

| # | Edit | Reading |
| - | ---- | ------- |
| 6 | The whole **solar-panel bullet** was cut from "Wat er in de prijs zit" | Reason not stated. Either it is no longer offered free by default, or it is pointless for an ultrasonic sensor inside a closed pit (`Docs/Sensor_Overzicht.md#zonnepaneel` says the panel is fixed to the module). **Unverified — see open questions.** |
| 7 | The whole **"Twee vragen"** section was cut (WiFi near the pit; obstacles inside the pit; pit dimensions) | Consistent with edit 1: those questions get asked on the phone. A quote mail should not end in a questionnaire. |

### Corrections

| # | Was | Became |
| - | --- | ------ |
| 8 | Shipping 6,53 € / 8,59 € (the `Docs/Prijzen.md` figures of 5,40 / 7,10 € × 1,21) | **9 € / 12 €**, round numbers |
| 9 | Installation row labelled *"Installatie ter plaatse (Mariakerke, < 100 km)"* | *"Installatie ter plaatse (Mariakerke)"* — the distance band is internal pricing logic, not something the customer needs to read |

### Two pricing decisions taken in the same session

* **Quotes to private customers are stated incl. 21 % VAT.** Instructed by Wim. This
  answers open question 5 of the 2026-08-16 file; an addendum has been added there.
* **Installation was quoted at 75 € excl. VAT (90,75 € incl.)** for a ~55 km trip, where
  `Docs/Prijzen.md` publishes 100 € for the < 100 km band. Instructed by Wim; whether this
  is a one-off or a new price is unresolved.

## Open questions

1. **Shipping: 9 € / 12 € or 5,40 € / 7,10 €?** `Docs/Prijzen.md` publishes the latter as
   bpost rates on a page that says all prices are excl. VAT. The quote used 9 / 12 € incl.
   Even grossed up, 5,40 → 6,53 and 7,10 → 8,59, so the published figures look stale, not
   merely VAT-exclusive. The public page has **not** been changed — it needs a decision.
2. **Installation price**: is 75 € excl. the new price for a ~55 km trip, or a one-off for
   this prospect? `Docs/Prijzen.md` still says 50 € (< 30 km) / 100 € (< 100 km).
3. **Solar panel**: is it still included free with a subscription, as
   `Docs/Prijzen.md` and `Docs/Sensor_Overzicht.md` both state? It was cut from this quote
   with no reason given.
4. Should the **neighbour-sharing** point about the LoRa gateway (edit 2) be published in
   `Docs/Sensor_Overzicht.md`? It materially widens when LoRa is an option, and it is
   currently only in Wim's head and in this quote.
5. Should quotes to **business** customers still be stated excl. VAT?

## Addendum 2026-09-09 — shipping resolved, two more edits

**Shipping is 9,00 € / 12,00 € incl. VAT** (7,44 € / 9,92 € excl.), and the rate now covers
**packaging as well as bpost postage** — that is why it went up. Confirmed by Wim;
`Docs/Prijzen.md` has been updated accordingly (both figures given, since that page quotes
excl. VAT throughout). Open question 1 is answered.

Two further edits Wim made to the same draft:

* Intro: *"Ik zag ook een gemiste oproep van u.  Sowieso bel ik je één dezer dagen terug
  op…"* — he answers a missed call inside the quote rather than separately.
* Subscription row: *"Abonnement, vanaf het tweede jaar, **dus na 12 maanden**"*. "Vanaf het
  tweede jaar" was read as ambiguous. The same clarification has been added to the
  *Abonnement* section of `Docs/Prijzen.md`.

Open questions 2 (installation 75 €) and 3 (solar panel) are still open.

## Addendum 2026-09-09 — installation band added, solar panel explained

Both remaining open questions answered by Wim.

**Installation (open question 2).** A new distance band was added rather than the 75 €
being a one-off. `Docs/Prijzen.md` now reads:

| Distance from Torhout | Price (excl. VAT) |
| --- | --- |
| < 30 km | 50 € |
| < 60 km | **75 €** (new) |
| < 100 km | 100 € |
| further | on request |

The 75 € quoted for the ~55 km trip is therefore the standard price, not a favour.

**Solar panel (open question 3).** Still free with a subscription — the customer-facing
pages are correct and stay as they are. It was cut from the quote **for simplicity**: a
cistern sensor sits underground, and the panel is fixed to the sensor module, so it is
"quasi nooit bruikbaar" there. Rule for quotes: mention it only where the module itself
gets daylight. Recorded in the runbook.

All open questions in this file are now closed.

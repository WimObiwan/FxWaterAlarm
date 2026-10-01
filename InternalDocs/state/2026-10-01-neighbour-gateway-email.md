# 2026-10-01 — Mail for a customer to forward to her neighbours (shared LoRa gateway)

## Problem

A private customer in Mariakerke (the prospect of
[`2026-09-09-quote-email-edits.md`](2026-09-09-quote-email-edits.md); invoice INV-000055,
417,45 € incl. VAT) now has an ultrasonic LoRa sensor in her rainwater pit and a LoRa
gateway in her garage. She offered to tell her neighbours, whose sensors could report
through her gateway. Wim asked for a mail she can forward: plain language, short, a
caveat that most pits work but not all, the exact price she paid, the return terms, and
links to the demo, the website and the photos.

A draft was generated and saved to Thunderbird's `Concept` folder. Wim edited it there and
sent it to the customer (Thunderbird `Verzonden`, 2026-10-01 21:54). This file records the
price basis and **what he changed**, so the next mail of this kind starts closer to the
version he actually sent.

## What was checked

* The customer's price, **read from her quote mail** (Zoho, 2026-09-12) and confirmed by the
  invoice total in her reply of 2026-10-01: sensor 169,40 € + gateway 157,30 € +
  installation 90,75 € = 417,45 €, subscription 36,30 € / year after 12 months, all incl.
  21 % VAT.
* A neighbour gets the same prices without the gateway: **260,15 € one-off, 36,30 € / year
  from month 13.** Derived from the above; Wim kept these figures unchanged.
* Diff between the draft (read back as text right after saving) and the sent mail (read
  from `Verzonden`). Provenance for the edit list: **read from the two versions.** The
  *readings* are inferred unless marked otherwise.

## Findings

### A referral deal for the gateway host

Wim replaced the one-line cover note to the customer with his own. The new part:

> Als er extra installaties zijn van je buren, zal ik ter compensatie je abonnement laten
> vallen voor een jaar, en dit tot 3x. Zie het als vergoeding voor het bakje dat in je
> garage hangt.

**One free subscription year for the gateway host per neighbour installation, at most
three.** It is a commitment to this customer and has to be applied when her subscription
is billed; it is in [`../backlog.md`](../backlog.md). Whether this is the standard offer
for any customer whose gateway serves neighbours is not stated (see open questions).

### Edits to the neighbour text

| # | Draft | Sent | Reading |
| - | ----- | ---- | ------- |
| 1 | *Beste buur,* | *Beste,* | More neutral; the reader may not consider themselves a neighbour of hers. |
| 2 | e-mail *als hij bijna vol of bijna leeg is* | *als hij bijna leeg is* | For a rainwater pit, running empty is what people care about. |
| 3 | *geen kastje, geen stroom en geen internet nodig aan de put* | *In de put heb je geen stroom en geen internet nodig. Dit werkt in principe tot een 100-tal meter ver.* | **A range figure: about 100 m.** Stated by Wim, not measured here. The "Past het in mijn put?" bullet still says *tot enkele huizen ver*; both are in the sent mail. |
| 4 | *Je eigen account: je ziet alleen je eigen put, en [host] ziet niets van de jouwe.* | *De WaterAlarm.be app met je eigen account (voor je eigen put).* | Privacy reassurance dropped. He calls the dashboard **"de app"**, here and in the demo link (#8). |
| 5 | *Je betaalt exact hetzelfde als [host], alleen het kastje valt weg: dat heeft zij al.* | *Je betaalt niet voor het kastje dat de metingen ontvangt (als het kastje van [host] je metingen kan ontvangen).* | No comparison with the host's price, and the free gateway is made **conditional on range**. |
| 6 | *Het eerste jaar zit in de prijs.* | *Het abonnement voor eerste jaar zit mee in de eenmalige prijs.* | Says explicitly *what* the first year is: the subscription. |
| 7 | *…nieuwe batterij als die leeg is (enkel de verzending komt erbij).* | *…nieuwe batterij als die leeg is. De batterij gaat in principe vele jaren mee.* | Shipping caveat dropped; **battery life is "many years"** added (`Docs/Sensor_Overzicht.md` gives ± 5 years for LoRa). |
| 8 | *Zo ziet het eruit (demo)* | *Zo ziet de app eruit (demo van echte waterput)* | The demo shows a real pit, not made-up data, and he says so. |
| 9 | *Willen meerdere buren meedoen, dan plan ik jullie graag op dezelfde dag in.* | removed | No promise about grouping visits. Installation stays 90,75 € per address. |
| 10 | — | Two images between the closing and the signature: `https://www.wateralarm.be/img/front/app.webp` (app screenshot showing a pit's volume) and `.../ultrasoon-put.webp` (ultrasonic sensor mounted in a pit) | A picture of the app and the sensor does more than a link. They are **remote images** from the frontpage, not attachments. |
| 11 | *een WaterAlarm in haar regenput* | *een WaterAlarm sensor in haar regenput* | Minor. |

Kept as drafted: the price table, the three limitations (clear view to the water, overflow
too high, distance to the gateway), the exchange and return terms, the three links, the
phone number.

## Open questions

1. **Is the referral deal standard?** One free subscription year per neighbour, up to
   three, for any customer whose gateway serves neighbours, or only for this customer?
   If standard, it belongs in the quote runbook and possibly in `Docs/Prijzen.md`.
2. **Range: "a 100 m or so" or "a few houses away"?** The sent mail says both. The runbook
   and the 2026-09-09 quote say "tot enkele huizen ver". Unmeasured either way.
3. **Remote images**: many mail clients block remote images until the reader allows them,
   so a neighbour may see only the alt text. Fine for a forward, worth knowing if this
   becomes a template.

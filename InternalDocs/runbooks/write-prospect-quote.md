# Write a quote e-mail to a prospect

How a WaterAlarm quote is written, distilled from the quotes actually sent. Prices come
from [`../../Docs/Prijzen.md`](../../Docs/Prijzen.md); this runbook covers everything that
page does not say. Sources: `state/2026-08-16-prospect-quote-email.md`,
`state/2026-09-09-quote-email-edits.md`.

## Before writing

1. **Read the prospect's mail for what is missing**, not just what is there. A one-line
   request ("offerte voor mijn waterput") leaves the two real choices open: ultrasonic vs.
   pressure, and LoRa vs. 5G.
2. **Check NB-IoT coverage for their address on the BIPT map** and say so in the mail,
   including that it is not a guarantee. Do this before quoting 5G. (Not "IBBT" — that
   merged into imec in 2012 and publishes no coverage map.)
3. **Re-read `Docs/Prijzen.md`** rather than quoting prices from memory or from an older
   mail; the price list has changed more than once.

## Prices

* **Private customers: quote incl. 21 % VAT.** Say so once: *"Alle prijzen zijn inclusief
  21 % btw."* Excl. VAT is the wrong frame for a consumer.
* State the recommended configuration as a small **table that totals to one "Eenmalig"
  figure**, with the subscription on a separate line below it — not folded into the total.
* Put the other combinations in a short **"Alternatieven"** table. Do not editorialise on
  them; the recommendation is the paragraph above the first table.
* **Don't show the customer the internal pricing logic.** *"Installatie ter plaatse
  (Mariakerke)"*, not *"(Mariakerke, < 100 km)"* — look the band up in `Docs/Prijzen.md`
  (50 € < 30 km, 75 € < 60 km, 100 € < 100 km, beyond that on request) and quote the
  amount, not the band.
* Shipping is **9,00 € to a postpunt / 12,00 € to an address** incl. VAT (7,44 € / 9,92 €
  excl.). It covers packaging as well as postage, which is why it is above the bare bpost
  rate.

## Who installs

**Self-installation is offered for the pressure sensor only.** For an ultrasonic sensor we
install on site. The reason goes in the mail, because it reads as a restriction otherwise:

> an ultrasonic sensor's accuracy depends on exactly where it hangs — an inlet pipe, a pump
> cable, a ladder or a protruding manhole rim gives a false echo — and that is visible and
> fixable on the spot. The customer does not end up with a sensor that mismeasures, and we
> do not end up swapping it afterwards.

A pressure cell just hangs in the water, so obstacles are irrelevant and self-install is
fine there. When the customer self-installs, add that **return shipping is at their
expense**.

## What to include, and what to leave out

Include:

* Links to `wateralarm.be/Docs/Sensor_Overzicht`, `/Docs/Prijzen`, `/demo` and `/photos`.
* A short "hoe het werkt" framing the two choices (what to measure / how to connect).
* That the **first year's subscription is in the purchase price** — write "vanaf het tweede
  jaar, dus na 12 maanden", because "vanaf het tweede jaar" alone gets misread — and what the
  subscription covers afterwards: platform, SIM and data for a 5G sensor, the mounting
  bracket, and a replacement battery **when it runs flat**, up to one per sensor per year,
  shipping not included.
* That a **LoRa gateway can be shared with neighbours, up to a few houses away** — it
  keeps LoRa on the table when there is no WiFi at the pit itself.
* The **exchange and return terms**, in that order. The exchange (pay only the price
  difference) is the stronger reassurance and the cheaper outcome for us; the return is the
  backstop. Say plainly that a return has never been needed and an exchange has.

Leave out:

* **A list of questions at the end.** Close with a phone call instead — and if there is a
  missed call from them, say so there rather than in a separate mail — *"Sowieso bel ik je
  één dezer dagen nog op om het kort te bespreken."* Details about the pit (obstacles,
  dimensions, WiFi nearby) get gathered on the phone, not in a form.
* **The solar panel.** It is genuinely still free with a subscription, but leave it out of
  the quote: a pit installation is underground, so the panel — which is fixed to the sensor
  module — is almost never usable. Mentioning it only adds a paragraph the customer has to
  rule out. Raise it only where the module actually sits in daylight.

## Delivering the draft

* Wim wants the draft as a **ready-to-import `.eml`**, not markdown in the terminal — see
  the `customer-emails-as-eml` note. `multipart/alternative`, plain + inline-styled HTML,
  Calibri/Arial 11pt, his Fox Innovations signature (an imported `.eml` gets no
  auto-signature). Copy the customer's `Message-ID` into `In-Reply-To`/`References`.
* Saving it straight into Thunderbird's `Concept` folder works too and is usually what he
  wants. **Every paragraph must be on one source line** or Thunderbird's editor glues words
  together across the line breaks. Since thunderbird-mcp 0.7.4 `saveDraft` takes
  `inReplyTo`/`references`, so the draft can thread (it could not before, 2026-10-01); pass
  the `Re:` subject yourself. With the signature already in the body, pass
  `includeSignature: false`.
* Read the saved draft back and check it before reporting done.

## Variant: a mail the customer forwards to neighbours

When a customer's LoRa gateway can serve the neighbours, write a short mail she can forward
(first done 2026-10-01, see `state/2026-10-01-neighbour-gateway-email.md`):

* A short note to the host on top, then the neighbour text. Greet the neighbours with
  *"Beste,"*, not *"Beste buur"*.
* **Same prices as the host, minus the gateway**, and make the free gateway conditional:
  *"(als het kastje van [host] je metingen kan ontvangen)"*. Do not compare with what the
  host paid. Installation is per address.
* The three limitations, briefly: clear view to the water, overflow very high, distance to
  the gateway. Wim gives the range as *"in principe tot een 100-tal meter"* (unmeasured).
* Battery: *"gaat in principe vele jaren mee"*. Call the dashboard **"de app"**, and the demo
  *"demo van echte waterput"*.
* Wim added the frontpage images `img/front/app.webp` and `img/front/ultrasoon-put.webp`
  under the text.
* Wim offered the host **one free subscription year per neighbour installation, up to three**,
  as payment for hosting the gateway. Whether that is standard is open; ask before offering it.

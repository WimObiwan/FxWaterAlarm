# Frontpage: merge blog.wateralarm.be into the Site

Status: **code done and verified locally.** The nginx change on server3 is still to do; see
[`../runbooks/retire-blog-subdomain.md`](../runbooks/retire-blog-subdomain.md).
Decision: [`../decisions/single-site.md`](../decisions/single-site.md).

## Problem

There were two sites. `www.wateralarm.be` is the ASP.NET Core app. `blog.wateralarm.be` is a
static frontpage built with Mobirise, and the app's `/` redirected to it. The frontpage was
out of date, had no integration with the app (Mijn sensoren, Inloggen, links into the Docs),
and depended on Mobirise. Question: is the split worth keeping?

## What was checked

- **Blog content vs `Docs/`** (*read from the live page, 2026-09-27*). The blog still showed
  early-bird prices, the gateway at 110 € (Docs: 130 €), "5G incl. 7-year subscription"
  (Docs: first year included, then 30 €/year), a "spring 2025" update, and an old FAQ and
  roadmap. Meanwhile `Docs/Prijzen.md` and `Docs/Sensor_Overzicht.md` were current.
- **The blog's contact form** posts to `https://mobirise.eu/` (formoid) (*read from the
  HTML*). It only works through Mobirise's service.
- **Analytics** (*read from the HTML and from `_Layout.cshtml`*): the blog uses GA
  `G-HEJ2ZM9CYB`, the app uses `G-QJTVPL3P9K`.
- **The `auto` cookie** is set host-only (`Site/Pages/Auto.cshtml.cs`, no `Domain`)
  (*read from code*). The blog subdomain cannot see it, so it cannot show "Mijn sensoren" to
  returning customers without a domain-wide cookie plus JS. The same applies to login state.
- **Redirect chain in production** (*verified with curl, 2026-09-27*):
  - `https://wateralarm.be/*` → 301 → `https://www.wateralarm.be/*`. **www is the canonical host.**
  - `https://www.wateralarm.be/` → 301 → `https://blog.wateralarm.be/`, **with the query string
    dropped**: `/?utm_source=x` also lands on the bare blog URL. UTM tags from QR codes were
    lost at this step. `Short.cshtml.cs` (QR codes) redirected straight to the blog with UTM
    tags, so that path did keep them.
- **`Site/Pages/Index.cshtml`** already existed (release notes v0.1–v0.14, an old roadmap), but
  production never served it because of the redirect (*read from code*).

## Findings

The split costs more than it gives. Its only real advantage is that a static page stays up
when Kestrel is down, and an nginx `error_page` fallback covers that. Merged:

- `/` is a Razor page in Site (`Site/Pages/Index.cshtml`), Dutch-only. It is indexable
  (`RobotsAllowIndex`), has a canonical link and OG tags pointing to `https://www.wateralarm.be/`,
  and links straight into `/Docs/...`.
- Prices on the frontpage are headline figures only ("vanaf 140 €", "30 €/jaar"), with a link
  to `Docs/Prijzen.md`, which stays the price list. When prices change, check the frontpage
  too.
- Returning visitors (`auto` cookie or logged in) get "Mijn sensoren" as the main button.
  New visitors get "Live demo", "Welke sensor?" and "Inloggen".
- Contact uses `mailto:`/`tel:` only. No form, so no spam handling and no new endpoint.
- The layout lost its "News blog" nav item. The brand now links to `/`.
- `Short.cshtml.cs` (QR codes) now redirects to `/?utm_...` instead of the blog.
- The blog's photos are copied to `Site/wwwroot/img/front/`.

Verified locally (*by running it*): `dotnet test` green (1080 tests). Every `href`/`src`
on `/` returns 200 or the expected redirect, and every `/Docs/...#anchor` exists. The hero
changes with the `auto` cookie. `/Short?c=qr|pst|th25|111` → `/?utm_source=qr&...`.
Screenshots checked at desktop and 390 px width.

**Gotcha found along the way:** scoped CSS (`Index.cshtml.css`) is not applied to an `<img>`
whose `src` uses `~/`. The URL-resolution tag helper drops the `b-xxxx` scope attribute. Use
plain `/img/...` paths on elements that need scoped styles.

## Open questions

- Dark mode not checked visually (headless Chrome does not pick up the theme switcher). The page only
  uses Bootstrap variables, except the light background behind the Docs drawings, which is
  deliberate because the drawings have a light background of their own.
- Is there a static fallback page for when Kestrel is down? Optional; see the runbook.
- After the switch, stop GA property `G-HEJ2ZM9CYB` once nothing reports into it anymore.

## Addendum 2026-09-27: prices incl. btw for consumers

Showing consumers prices only exclusive of btw is not allowed. The source text stays
**exclusive** of btw (`Docs/*.md`, the frontpage). This keeps the `/mcp` endpoint and B2B
quotes simple. A Particulier / Professioneel switch in `Site/wwwroot/js/site.js` rewrites the
text in the browser:

- Scope: elements with `data-vat-scope` (the frontpage "Prijzen" section, where
  `data-vat-toggle` marks where the switch goes). Also every Docs page whose text contains a
  "123 €" amount: the switch goes under its `h1` (today: Prijzen, Sensor_Overzicht,
  Druksensor_Opstelling).
- Particulier (the default) multiplies each amount by 1.21 and turns "exclusief btw" into
  "inclusief btw". The choice is stored in `localStorage` (`vatMode`) and shared across pages.
- **Rule for writing Docs:** always write amounts exclusive of btw, as `123 €` or `7,44 €`.
  Never also write an amount inclusive of btw next to it, because the switch would convert it a
  second time. (The shipping bullet "Inclusief btw komt dat neer op 9,00 €…" was removed for
  that reason.)
- Without JavaScript, the page shows the exclusive amounts together with "exclusief btw", which
  is at least consistent.

Verified by driving Chrome over CDP: both modes on the frontpage and on /Docs/Prijzen, the
choice remembered across pages, 7,44 € → 9,00 €, and 140 € → 169,40 €.

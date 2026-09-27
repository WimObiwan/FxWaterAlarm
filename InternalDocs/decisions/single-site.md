# Single site: the frontpage lives in the Site app

Decided 2026-09-27. How it was reached: [`../state/2026-09-27-frontpage-merge.md`](../state/2026-09-27-frontpage-merge.md).

## Decision

WaterAlarm has one website, `https://www.wateralarm.be`, served by the Site app. The public
frontpage is `Site/Pages/Index.cshtml`. Customer documentation is `Docs/` (served under
`/Docs/`). `blog.wateralarm.be` only 301-redirects to `www.wateralarm.be`, keeping the path
and query string. There is no separate marketing site and no site builder.

## Why

- **One source of truth.** The Mobirise blog drifted from `Docs/` (old prices, old
  subscription model, old FAQ) because nobody edits two places. The frontpage now shows only
  headline figures and links to the Docs for everything detailed.
- **Integration needs the same origin.** "Mijn sensoren" for returning visitors and the login
  state come from host-only cookies on www. A subdomain can't see them.
- **No lock-in.** The blog's contact form posted to Mobirise's service, and its analytics
  were a separate GA property.
- **SEO.** Frontpage and Docs on one host, with a canonical URL.

## Rejected

- **Keep the static blog, just refresh the content.** It fixes the drift once, not the cause.
- **A different static site generator on the subdomain.** Same cookie/origin problem, and a
  second build and deploy to maintain.
- **Frontpage as a markdown page in `Docs/`.** Markdown can't do the conditional hero
  (cookie/login), and it would turn a landing page into a doc page with breadcrumbs.
- **A contact form.** Needs spam protection and a mail endpoint. `mailto:`/`tel:` is enough
  for the volume.

## Consequences

- When prices change in `Docs/Prijzen.md`, check the headline figures on the frontpage
  (section "Prijzen" in `Index.cshtml`).
- The frontpage is only as available as Kestrel. If that matters, add an nginx `error_page`
  fallback (see [`../runbooks/retire-blog-subdomain.md`](../runbooks/retire-blog-subdomain.md)).

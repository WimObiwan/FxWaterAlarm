---
paths:
  - "Docs/**"
  - "InternalDocs/**"
  - "Site/wwwroot/Docs/**"
---

# Documentation conventions

Two separate documentation trees. They have different audiences, different languages and
different rules. Never move content between them without rewriting it for the other audience.

| Tree | Audience | Language | Published? |
| ---- | -------- | -------- | ---------- |
| `Docs/` | customers and installers | **Dutch** | yes — live on https://wateralarm.be/Docs/ |
| `InternalDocs/` | us (developer/ops notes) | **English** | no — repo only |

## `Docs/` — customer-facing

**Dutch, always.** Even when the source material, the ticket or the conversation is in
English. Product/UI terms keep their in-app spelling.

**Markdown only** (`.md`), plus images alongside the page that uses them.

**Filenames are the URL and the navigation label.** CamelCase words, underscores instead of
spaces, `.md` extension — `Versie_Informatie.md`, `Sensor_Nodes/`, `Home_Assistant.md`.
`Site/Pages/__MarkdownPageTemplate.cshtml` builds the breadcrumb by replacing `_` with a
space, so `Sensor_Nodes/DDS75-LB.md` renders as *Sensor Nodes › DDS75-LB*. No spaces, no
lowercase-only names, no `-` as a word separator (hyphens are fine *inside* a product name
like `DDS75-LB` or `NB-IOT`).

Keep the file's first `# heading` in agreement with the filename — the heading becomes the
HTML `<title>`, the filename becomes the breadcrumb and the URL.

**Renaming a page breaks its public URL.** Customers, e-mails and the MCP endpoint link to
these paths. Rename only when asked, and say that links will break. A handful of legacy
files predate this convention (`Docs/Aanpassingen/mangat-volume-compensatie.md`,
`Docs/3D-designs/`, `Docs/_Admin/NB-IOT/`); leave them alone unless renaming is the task.

**Every folder needs an `index.md`** listing its children — that is the folder's landing
page. Add the new page to the parent `index.md` in the same change; an unlinked page is
only reachable by guessing the URL.

**How it is served.** `Site/wwwroot/Docs` is a symlink to `../../Docs`, and
`Site/Program.cs` registers `config.AddMarkdownProcessingFolder("/Docs/", ...)`. Files are
rendered live from disk — no build step, no page registration, no route to add. Dropping a
`.md` file in `Docs/` publishes it at the matching URL on the next request. Edit `Docs/`,
never the symlink path.

`Site/Services/McpDocumentationService.cs` also indexes this tree for the `/mcp`
documentation endpoint, so anything here is machine-readable for third parties too.

**Everything under `Docs/` is public.** `_Admin/` is merely unlisted from `index.md` — it
is *not* authenticated. No credentials, no customer data, no account or sensor links, no
internal hostnames.

## `InternalDocs/` — technical / ops notes

English. Not served by the site, not symlinked into `Site/`. This is the project's lab
notebook, in the spirit of the `homelab-ops` workspace.

```
InternalDocs/
  README.md      layout + conventions (read it before adding files)
  state/         dated findings — one file per investigation thread
  decisions/     durable decisions, one per file, with the reasoning
  runbooks/      procedures to follow: releases, migrations, incidents
  backlog.md     loose ideas and todo's that have no home yet
```

Naming here is lowercase-kebab, not the `Docs/` CamelCase style, because these filenames
are never rendered as titles: `state/YYYY-MM-DD-<topic>.md`,
`decisions/<topic>.md`, `runbooks/<verb>-<topic>.md`.

**Write findings down as you go.** When an investigation produces conclusions, they go in
`state/YYYY-MM-DD-<topic>.md`, structured problem → what was checked → findings → open
questions. Append to the existing file when it is the same thread rather than starting a
new one.

**Record provenance.** Distinguish "verified by running it" from "read from the docs" from
"reported, unverified". That distinction is what lets a wrong conclusion be corrected later.

**Promote what lasts.** A conclusion that keeps being reread belongs in `decisions/` or
`runbooks/`; the dated `state/` file stays as the record of how it was reached.

**Read before write.** Check for an existing file on the same thread before creating a new
one, and don't rewrite history in `state/` — correct it with a dated addendum.

**Still no secrets.** The GitHub repo is the boundary, not the website: record *that* a
credential exists and where it lives, never the value.

# InternalDocs

Technical documentation for WaterAlarm: findings, decisions, procedures and loose ends.
**English**, markdown, developer/ops audience.

This tree is *not* published. It lives in the repo only — no symlink into `Site/`, no route,
not indexed by the `/mcp` documentation endpoint. Customer-facing documentation is Dutch and
lives in [`../Docs/`](../Docs/); see [`../.claude/rules/documentation.md`](../.claude/rules/documentation.md)
for the rules on both trees.

## Layout

| Path | What it holds |
| ---- | ------------- |
| `state/` | Dated findings — one file per investigation thread, `YYYY-MM-DD-<topic>.md` |
| `decisions/` | Durable decisions, one per file, `<topic>.md` — what was decided, why, what was rejected |
| `runbooks/` | Procedures to follow: releases, migrations, incident handling, `<verb>-<topic>.md` |
| `backlog.md` | Ideas and todo's that have no home yet |

Filenames are lowercase-kebab. (The CamelCase-with-underscores rule applies to `Docs/`
only, where the filename becomes the page title and URL.)

## Conventions

**`state/` is a lab notebook, not a landfill.** One file per investigation, structured
problem → what was checked → findings → open questions. Append to an existing file when it
is the same thread rather than opening a new one. Dates are the date of the work, in
`YYYY-MM-DD`; never relative ("last week", "yesterday").

**Record how you know.** Mark each finding as verified by running it, read from
documentation, or reported but unverified. Several conclusions in this repo were wrong at
first, and provenance is what made them correctable.

**Promote what lasts.** When a conclusion stops being a finding and starts being how we do
things, move it into `decisions/` or `runbooks/`. Leave the `state/` file in place as the
record of how it was reached, with a pointer.

**Don't rewrite history.** Correct an old `state/` file with a dated addendum, not by
editing the original conclusion away.

**No secrets.** Note *that* a credential exists and where it lives, never its value — the
GitHub repo is the boundary here, not the website.

## History

Before 2026-08-14 this directory was flat, with files named `2025-05-a-…` (the `2025` was a
typo; the work is from 2026-05). They were renamed and moved into `state/` when this layout
was introduced.

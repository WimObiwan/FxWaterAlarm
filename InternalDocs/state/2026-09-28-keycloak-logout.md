# Rejected or logged-out OIDC login leaves the Keycloak session alive

Status: **code done, unit-tested, deployed to dev 2026-09-28 (incl. the email-claim fix below). Post-logout URIs configured; Google `select_account` and the dev test still to do.**
Follows [2026-09-24-keycloak-sso](2026-09-24-keycloak-sso.md).

## Problem

1. "Aanmelden" → Keycloak → Google → a Google account WaterAlarm does not know.
2. WaterAlarm correctly shows `Error_no_account`, but only redirects to `/login?error=…`.
3. The Keycloak SSO session stays alive. The next "Aanmelden" is answered by Keycloak
   without any page, with the same identity → the same error, in a loop. There is no way
   to pick email-code login or another Google account.

Plain logout had the same root cause: `AccountCallback` only cleared `WaterAlarm.Auth`, so
"Aanmelden" after "Afmelden" silently signed the same identity back in.
*Established by reading the code* (`OidcCallback`, `AccountCallback`).

## Change: RP-initiated logout at Keycloak

- `Site/Authentication/OidcSession.cs` — new helper. `SignOutOfProvider(idToken, redirect)`
  returns a `SignOutResult` for the `oidc` scheme carrying the id_token as
  `id_token_hint`, or a plain redirect when there is no id_token (email-code login, legacy
  Google session, cookies issued before this change).
- `Site/Program.cs`
  - `OnTokenValidated` keeps the **id_token only** in the auth properties. `SaveTokens` stays
    off, so access and refresh tokens are still not carried around.
  - `OnRedirectToIdentityProviderForSignOut` sets `IdTokenHint` from the properties. It is
    needed because the handler's own lookup reads the `ExternalCookie`, which the callback
    has already consumed. The item is removed there, so the token doesn't end up again in
    the `state` parameter.
- `OidcCallback` — every rejection after a successful Keycloak round trip
  (`email_not_verified`, missing `sub`, `no_account` ×2, link-mode `not_authenticated` /
  `no_account` / `oidc_conflict`) goes through Keycloak logout before it lands on its page.
  A successful sign-in stores the id_token in the `WaterAlarm.Auth` ticket properties.
- `AccountPicker` — the id_token goes through the picker cookie (`AccountPickerToken.IdToken`)
  and is kept across an account switch.
- `AccountCallback` (logout) — reads the id_token before clearing the cookie. An OIDC session
  also logs out at Keycloak, then lands on the same return URL as before.

Tests: `SiteTests/Authentication/OidcSessionTest.cs` and
`AccountCallbackTest.OnGet_EmptyToken_OidcSession_AlsoSignsOutOfKeycloak`.

## Keycloak configuration needed (not in code)

Both on `wateralarm-dev` **and** `wateralarm-prd` (realm `foxinnovations`, `auth.foxinnovations.be`).

### 1. Valid post logout redirect URIs (required, or logout fails)

The OIDC handler sends `post_logout_redirect_uri = <root>/signout-callback-oidc`, then
redirects from there to the page we asked for. Keycloak only accepts registered URIs, and the
login redirect URIs (`/signin-oidc`) do not count for logout.

**Done 2026-09-28 on both clients:** Clients → `wateralarm-dev` / `wateralarm-prd` → Settings →
*Login settings* → **Valid post logout redirect URIs** = `/*`. *Configured by Wim.*

Keycloak resolves a relative URI against the client's Root URL, so this means
`https://dev.wateralarm.be/*` and `https://www.wateralarm.be/*`: any path on the own host,
which covers `/signout-callback-oidc`. That's broader than the one path needed, but it isn't an open
redirect: no other host is allowed. It depends on the Root URL being set on both clients,
which it is (see the 2026-09-24 note). The strictest value would be `/signout-callback-oidc`.

(`+` would mean "same as the redirect URIs", which does not cover this path.)
Without a matching entry, Keycloak shows *"Invalid redirect uri"* on every rejection and
every logout of an OIDC session.

### 2. Google identity provider: prompt = select_account (required for the reported case)

Ending the Keycloak session is not enough on its own. Google keeps its own browser session
and, with one Google account signed in, silently hands the same account back. We don't log
users out of Google itself.

Identity providers → `google` → *Advanced settings* → **Prompt** → `select_account`.

This is realm-wide, so it also applies to the other apps brokering Google (FilmOpTV): users
will see Google's account chooser on each login that reaches Google. That's one extra click,
and only when there is no live Keycloak session.

## To verify on dev

- [ ] Unknown Google account → message on `/login`, no Keycloak confirmation page; the next
      "Aanmelden" shows the Keycloak login page, and Google shows the account chooser.
- [ ] Known account → logout → "Aanmelden" shows the Keycloak login page again.
- [ ] Logout **after the id_token has expired** (Keycloak access/id token lifespan is minutes,
      our cookie lives `TokenLifespan`). I expect Keycloak to accept an expired
      `id_token_hint`, since it checks the signature for logout, but this is *not verified*. If
      it refuses, the user ends up on a Keycloak error page on logout. Fallback then: send
      `client_id` instead of the hint and accept the confirmation page.
- [ ] Logout after the Keycloak SSO session has already expired (default SSO session max 10 h).
- [ ] Sessions from before the deploy have no id_token → logout stays local-only (as before).

## Open questions

- Every rejected Google account still leaves a user behind in the Keycloak realm (first broker
  login). Harmless; clean up occasionally, or revisit if users are ever pre-provisioned.
- Pre-existing, unrelated: the logout `url` parameter in `AccountCallback` is redirected to
  without an `IsLocalUrl` check, so it's an open redirect on logout. It's still there, now also as the
  post-logout `RedirectUri`.

## 2026-09-28 — found while testing: OIDC session carried the account's email, not the user's

**Problem.** Wim added `liesbethobiwan@gmail.com` as a user of account 1 on dev and logged in
with that Google account. The navbar and the Info page still showed `wim@obiwan.be`.

**Checked.** The dev log showed that Keycloak asserted the right identity and the account match
was right: `OIDC callback: email=liesbethobiwan@gmail.com` →
`OIDC login succeeded for email wim@obiwan.be (account 1)`. *Established from the log.*

**Finding.** `OidcCallback.SignInAccount` and `AccountPicker.SignInAccount` put
`account.Email` in the session's `email` claim, while the email-code login
(`AccountCallback`) puts the user's own address there. The rest of the app reads that claim as
the login identity. So every OIDC user of an account was treated as the account's owner:

- `UserInfo.CanUpdateAccount` / `GetAccessibleAccounts` and the account picker gave access to
  **every account where the owner's address is a Mail user**, not only the account the user
  was added to.
- `AdminRequirement`: if an account's main address is in `AdminEmails`, any OIDC user of that
  account passed the admin-by-email check (the `AdminIPs` restriction still applies).
- Audit entries named the owner as the actor.

This dates from the original Google login (`6cde4c3`, `8b73649`), not from the Keycloak switch.
*Established by reading the code.* Prd's `AdminEmails`/`AdminIPs` were not checked, so how
exposed prd is was not established.

**Fix.** `email` is now the verified address Keycloak asserted (`OidcCallback`), the picker
token's email (first login via the picker), or the session's current email (account switch).
The active account is still `sub` = account Uid. The log line now shows both addresses.
Tests: `SiteTests/Pages/OidcCallbackTest.cs`.

Sessions issued before the deploy keep the old claim until they expire (`TokenLifespan`),
unless the data-protection keys are rotated or the users log in again.

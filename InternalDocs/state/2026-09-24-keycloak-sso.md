# Migrate Site login from direct Google OAuth to Keycloak (OIDC)

Status: **live on dev and verified.** Production config is in place; only the deploy remains.

## Problem

The estate is consolidating external login on Keycloak (`auth.foxinnovations.be`, realm
`foxinnovations`), which already brokers Google, Entra and legacy Auth0. FilmOpTV moved on
2026-09-23/24. WaterAlarm still spoke to Google directly, so it was the last app holding its
own IdP client.

## What was checked first

- `Site/Program.cs` wired `AddGoogle` with `SignInScheme = "ExternalCookie"`, callback
  `/signin-google`, then handed off to the `/GoogleCallback` page, which does the real work
  (manual `SignInAsync` into the `WaterAlarm.Auth` cookie). The email-link login and the
  `ApiKey` scheme are untouched by this change.
- **Production data** (`/var/www/www.wateralarm.be/WaterAlarm.db`, read-only):
  27 accounts, 27 `AccountUser` rows of `LoginType.Mail`, and only **2** of
  `LoginType.Google`. *Established by query.*

| account | Mail row | Google row | Keycloak asserts | email fallback |
| ------- | -------- | ---------- | ---------------- | -------------- |
| 1 | `wim@obiwan.be` | `wimobiwan@gmail.com` | `wim@obiwan.be` | matches |
| 19 | `gery.levrouw@gmail.com` | same | `gery.levrouw@gmail.com` | matches |

## Key finding: no database migration is needed

`AccountsByEmailQuery` matches **only `LoginType.Mail` rows**. So when a login arrives with a
subject that no `AccountUser` row knows (which is exactly what happens once the subject is
Keycloak's rather than Google's), `HandleLoginMode` falls through to the email lookup, finds
the account via its Mail row, and **auto-links** the new identity for next time.

This is why WaterAlarm needs nothing like the FilmOpTV subject rewrite: the accounts re-link
themselves on first login. It holds because every account has a Mail row. *Established by
query.* Both existing Google users resolve via the table above.

## Changes

- `Core/Entities/AccountUser.cs` — **added** `AccountUserLoginType.Oidc = 2`. `Google = 1` is
  kept and still honoured everywhere, so the two existing rows keep working and rollback is
  a redeploy rather than a data fix. No EF migration: the column is an int that already exists.
- `Site/Authentication/GoogleAuthOptions.cs` → `OidcOptions.cs`, config section
  `GoogleAuth` → **`Oidc`**, gaining `Authority`.
- `Site/Program.cs` — `AddGoogle` → `AddOpenIdConnect("oidc", …)`: code flow + PKCE,
  `SignInScheme` still `ExternalCookie`, callback `/signin-google` → **`/signin-oidc`**,
  scopes explicitly `openid profile email` (`email` is not in the default set and the
  callback requires it), `GetClaimsFromUserInfoEndpoint = true`, `SaveTokens = false`.
- `Site/Pages/GoogleCallback.*` → `OidcCallback.*`; provider string `"google"` → `"oidc"`.
- Authorization checks in `UserInfo`, `AccountUsers` and `OidcCallback` now compare
  `u.Provider == <session provider claim>` instead of hardcoding `"google"`, and accept both
  `Oidc` and `Google` login types. Legacy sessions therefore survive the deploy (the data
  protection keys persist, so existing auth cookies stay valid).
- `Site/Site.csproj` — `Microsoft.AspNetCore.Authentication.Google` →
  `…Authentication.OpenIdConnect`.
- Resource strings and the login card are now provider-neutral ("single sign-on"), EN + NL.
  The `Google` label is retained for displaying legacy rows.

### Two deliberate behaviour changes

1. **Unverified email is now rejected.** The old code treated a *missing* `email_verified`
   claim as verified, reasoning that Google always verifies. That reasoning does not carry
   over: Keycloak brokers arbitrary identity providers, and the realm already contains a user
   with `EMAIL_VERIFIED=0`. A missing or non-`true` claim is now a rejection.
2. **Open redirect closed.** `returnUrl` arrives from the `r` query parameter and round-trips
   through the provider, so it is attacker-controllable. It was passed unchecked to
   `Redirect()` in `GoogleCallback.SignInAccount`, `AccountPicker.SignInAccount` and
   `Login.OnGet`. All three now use `Url.IsLocalUrl(...) ? ... : "/auto"`, and `Login.OnGetOidc`
   drops a non-local value before it is put into the auth properties at all. Same bug class as
   the FilmOpTV fix of 2026-09-24.

### Tests

`SiteTests/Utilities/UserInfoTest.cs` gains `CanUpdateAccount_ReturnsTrue_WhenOidcProviderMatches`
and `CanUpdateAccount_ReturnsFalse_WhenSubjectMatchesButProviderDiffers` — the latter pins the
rule that a legacy `google` row must not authorize an `oidc` session carrying the same subject
string. The existing Google test is unchanged and still passes.

## Before this can be deployed

1. ~~Keycloak clients~~ — **done 2026-09-25.** `wateralarm-dev` and `wateralarm-prd` exist,
   confidential, standard flow, root URLs `https://dev.wateralarm.be` /
   `https://www.wateralarm.be`, redirect `/signin-oidc` (relative, resolved against the root
   URL). The `email` scope is a default scope on both and carries the `email verified` mapper,
   so the stricter `email_verified` check above is safe.
2. ~~Host config~~ — **done 2026-09-25.** An `Oidc` section was written to
   `/var/www/dev.wateralarm.be/appsettings.Local.json` and
   `/var/www/www.wateralarm.be/appsettings.Local.json` on server3, with backups.
   `GoogleAuth` was left in place so a rollback to this build still works. No restart was
   needed — `AddJsonFile(..., reloadOnChange: true)` and the section is inert for the deployed
   binary. Details in the ops workspace,
   `state/2026-09-25-wateralarm-keycloak-sso.md`.
3. **Still to do: deploy this build to `dev.wateralarm.be`, test, then production.**
   `Site/appsettings.Local.json` on the *development machine* still carries only `GoogleAuth`;
   it needs an `Oidc` section for local runs.

## Open questions

- Account 1's Google row (`wimobiwan@gmail.com`) will remain as a dead `LoginType.Google` row
  after the switch, since the new link is written under `oidc` with the `wim@obiwan.be`
  identity. Harmless, but worth deleting once the new path is confirmed.
- Nothing forces the two stale `Google` rows to be cleaned up; decide after dev testing.

## 2026-09-25 — dev deploy #1 failed, fixed

`Error_oidc_failed` after the provider round trip. Keycloak logged three clean `LOGIN` events,
so the provider side was fine; the app log showed
`OpenIdConnectHandler[15] '.AspNetCore.Correlation.<id>' cookie not found.` and nginx showed
**`POST /signin-oidc`**.

`OpenIdConnectOptions.ResponseMode` defaults to `form_post`, so the code comes back as a
cross-site POST — on which a `SameSite=Lax` cookie is not sent. The `AddOpenIdConnect` block
had inherited `CorrelationCookie.SameSite = Lax` from the old `AddGoogle` block, where it was
correct because OAuth2 returns via GET.

Fixed: `CorrelationCookie` and `NonceCookie` are now `SameSiteMode.None` +
`CookieSecurePolicy.Always` — which is what ASP.NET Core defaults them to, and what FilmOpTV
(same flow, working) relies on. **Redeploy to dev required.**

## 2026-09-25 — dev verified

Deploy #2 works. `AccountUser` row 36 was created automatically:
`AccountId 1 | LoginType Oidc | wim@obiwan.be | oidc | d59feb2c…`, and the log shows
`email_verified=true` and `OIDC login succeeded ... (account 1)`.

The email-fallback re-link described above is confirmed end to end — no data migration was
needed. Production is ready to deploy; its config is already on server3.


## 2026-09-26 — admin via Keycloak client role

Admin was only `AccountLoginMessage:AdminEmails` (+ `AdminIPs`). Added the FilmOpTV pattern
(Keycloak roles lifted from the access token into `ClaimTypes.Role`), adapted:

- **Client roles only.** `Site/Authentication/KeycloakRoles.cs` reads
  `resource_access.{ClientId}.roles` and deliberately ignores `realm_access`: a realm role
  called `admin` meant for another app must not grant WaterAlarm admin. Because dev and prd
  are separate clients, admin is granted per environment.
- **Read in `OnTokenValidated`** (`Site/Program.cs`), from `TokenEndpointResponse.AccessToken`.
  `SaveTokens` stays `false` — FilmOpTV reads it later from the saved tokens, we don't keep them.
- **Carried into the app cookie.** WaterAlarm builds the `WaterAlarm.Auth` principal by hand,
  so `OidcCallback.SignInAccount` copies the role claims; `AccountPickerToken` carries them
  through the multi-account picker, and an account switch keeps the session's roles.
- `AdminRequirementHandler`: admin = role `admin` **or** listed email; `AdminIPs` still applies
  to both. Only a role-typed claim counts (same lesson as the FilmOpTV `ClaimChecker` fix).
- A revoked role stays effective until the cookie expires (`TokenLifespan`); there is no
  refresh against Keycloak.

*Established by unit tests only* (`KeycloakRolesTest`, `AdminRequirementHandlerTest`) —
not yet exercised against Keycloak.

Still to do: create client role `admin` on `wateralarm-dev` / `wateralarm-prd`, assign it,
check the `roles` client scope is a default scope on both, deploy, and verify on dev
(the callback log line now includes `roles=`). Then decide whether `AdminEmails` can shrink.

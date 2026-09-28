using System.Security.Claims;
using System.Text.Json;
using Core.Queries;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Core.Audit;
using Core.Commands;
using Core.Entities;
using Site.Authentication;

namespace Site.Pages;

public class OidcCallback : PageModel
{
    /// <summary>Provider name stored in AccountUser.Provider for Keycloak-brokered logins.</summary>
    internal const string ProviderName = "oidc";

    internal const string PickerCookieName = "WaterAlarm.Picker";
    internal const string PickerProtectionPurpose = "AccountPicker.OidcToken";

    private readonly IMediator _mediator;
    private readonly IAuditService _auditService;
    private readonly ILogger<OidcCallback> _logger;
    private readonly IDataProtectionProvider _dataProtectionProvider;

    public OidcCallback(IMediator mediator, IAuditService auditService, ILogger<OidcCallback> logger,
        IDataProtectionProvider dataProtectionProvider)
    {
        _mediator = mediator;
        _auditService = auditService;
        _logger = logger;
        _dataProtectionProvider = dataProtectionProvider;
    }

    public async Task<IActionResult> OnGet(
        [FromQuery(Name = "r")] string? returnUrl,
        [FromQuery] string? mode,
        [FromQuery(Name = "a")] string? accountLink,
        [FromServices] IConfiguration configuration)
    {
        using var auditScope = _auditService.BeginAction(AccountCallback.AuditActionLogin);
        await _auditService.LogAsync(AuditOutcome.Attempted);

        var result = await HttpContext.AuthenticateAsync("ExternalCookie");
        if (!result.Succeeded)
        {
            _logger.LogWarning("OIDC callback: external authentication result missing or invalid from IP {IpAddress}",
                HttpContext.Connection.RemoteIpAddress);
            await _auditService.LogAsync(AuditOutcome.Failed, new AuditDetails { Reason = "OIDC authentication failed" });
            return RedirectToPage("/Login", new { error = "oidc_failed" });
        }

        // Consume the external cookie immediately
        await HttpContext.SignOutAsync("ExternalCookie");

        // From here on the user has a Keycloak session. Every rejection below must end it
        // (Reject), or the next login attempt silently returns this same identity again.
        var idToken = OidcSession.GetIdToken(result.Properties);

        // MapInboundClaims (on by default) rewrites sub/email to the long ClaimTypes URIs.
        // Read the raw names too, so this keeps working if that is ever turned off.
        var subject = result.Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? result.Principal?.FindFirstValue("sub");
        var email = result.Principal?.FindFirstValue(ClaimTypes.Email)
                    ?? result.Principal?.FindFirstValue("email");
        var emailVerified = result.Principal?.FindFirst("email_verified")?.Value;
        // Keycloak client roles, lifted into role claims in Program.cs (OnTokenValidated).
        var roles = result.Principal?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList() ?? [];

        _logger.LogInformation(
            "OIDC callback: email={Email}, email_verified={EmailVerified}, roles={Roles} from IP {IpAddress}",
            email, emailVerified, roles, HttpContext.Connection.RemoteIpAddress);

        // Keycloak emits email_verified as a JSON boolean, which surfaces as "true"/"false"
        // with provider-dependent casing. Treat a missing claim as unverified: unlike Google,
        // a brokered identity provider gives no guarantee that the address was checked.
        if (string.IsNullOrEmpty(email)
            || !string.Equals(emailVerified, "true", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "OIDC login rejected: email not present or not verified (email={Email}, email_verified={EmailVerified})",
                email, emailVerified);
            await _auditService.LogAsync(AuditOutcome.Denied,
                new AuditDetails { Reason = "Email address not present or not verified" },
                target: new AuditTarget { Email = email });
            return Reject(idToken, "email_not_verified");
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            _logger.LogWarning("OIDC callback: missing sub claim from IP {IpAddress}",
                HttpContext.Connection.RemoteIpAddress);
            await _auditService.LogAsync(AuditOutcome.Failed,
                new AuditDetails { Reason = "Missing sub claim" },
                target: new AuditTarget { Email = email });
            return Reject(idToken, "oidc_failed");
        }

        if (mode == "link" && !string.IsNullOrEmpty(accountLink))
            return await HandleLinkMode(accountLink, subject, email!, idToken);

        return await HandleLoginMode(returnUrl, subject, email!, roles, idToken, configuration);
    }

    private async Task<IActionResult> HandleLoginMode(
        string? returnUrl,
        string subject,
        string email,
        IReadOnlyList<string> roles,
        string? idToken,
        IConfiguration configuration)
    {
        // Check the direct provider link first
        var linkedUser = await _mediator.Send(new AccountUserByProviderQuery
        {
            Provider = ProviderName,
            ProviderSubjectId = subject
        });

        if (linkedUser != null)
        {
            var linkedAccount = await _mediator.Send(new AccountByIdQuery { Id = linkedUser.AccountId });
            if (linkedAccount == null)
            {
                _logger.LogWarning("OIDC login: linked account not found for sub {Sub}", subject);
                await _auditService.LogAsync(AuditOutcome.Failed,
                    new AuditDetails { Reason = "Linked account not found" },
                    target: new AuditTarget { Email = email });
                return Reject(idToken, "no_account");
            }
            return await SignInAccount(linkedAccount, subject, email, roles, idToken, returnUrl, configuration);
        }

        // Fall back: look for all mail AccountUsers matching the asserted email.
        // This is what lets a user whose old direct-Google link is no longer matched
        // (the subject is now Keycloak's, not Google's) re-link themselves on first login.
        var accounts = await _mediator.Send(new AccountsByEmailQuery { Email = email });

        if (accounts.Count == 0)
        {
            _logger.LogWarning("OIDC login: no WaterAlarm account found for email {Email} / sub {Sub}", email, subject);
            await _auditService.LogAsync(AuditOutcome.Denied,
                new AuditDetails { Reason = "Unknown email address" },
                target: new AuditTarget { Email = email });
            return Reject(idToken, "no_account");
        }

        if (accounts.Count == 1)
        {
            var account = accounts[0];
            // Auto-link this identity to the matched account for next logins
            await _mediator.Send(new AddAccountUserCommand
            {
                AccountId = account.Id,
                LoginType = AccountUserLoginType.Oidc,
                Email = email,
                Provider = ProviderName,
                ProviderSubjectId = subject
            });
            return await SignInAccount(account, subject, email, roles, idToken, returnUrl, configuration);
        }

        // Multiple accounts match — redirect to picker
        _logger.LogInformation(
            "OIDC login: multiple accounts ({Count}) found for email {Email}, redirecting to picker",
            accounts.Count, email);

        var token = new AccountPickerToken
        {
            ProviderSub = subject, Email = email, Roles = roles, IdToken = idToken, ReturnUrl = returnUrl
        };
        var protector = _dataProtectionProvider.CreateProtector(PickerProtectionPurpose);
        var protectedToken = protector.Protect(JsonSerializer.Serialize(token));

        Response.Cookies.Append(PickerCookieName, protectedToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromMinutes(10),
            Path = "/"
        });

        return RedirectToPage("/AccountPicker");
    }

    private async Task<IActionResult> HandleLinkMode(
        string accountLink,
        string subject,
        string providerEmail,
        string? idToken)
    {
        // Must already be signed in
        var session = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);
        if (!session.Succeeded)
            return Reject(idToken, "not_authenticated");

        var account = await _mediator.Send(new AccountByLinkQuery { Link = accountLink });
        if (account == null)
            return Reject(idToken, "no_account");

        // Verify current session user is authorized on this account
        var sessionEmail = session.Principal?.FindFirstValue("email");
        var sessionProvider = session.Principal?.FindFirstValue("provider");
        var sessionProviderSub = session.Principal?.FindFirstValue("provider_sub");

        var accountUsers = await _mediator.Send(new AccountUsersByAccountQuery { AccountId = account.Id });
        var isAuthorized = accountUsers.Any(u =>
            (u.LoginType == AccountUserLoginType.Mail
                && sessionEmail != null
                && string.Equals(u.Email, sessionEmail, StringComparison.OrdinalIgnoreCase))
            || (u.LoginType is AccountUserLoginType.Oidc or AccountUserLoginType.Google
                && sessionProvider != null
                && u.Provider == sessionProvider
                && sessionProviderSub != null
                && u.ProviderSubjectId == sessionProviderSub));

        if (!isAuthorized)
            return Forbid();

        // Check if this subject is already linked anywhere
        var existingLink = await _mediator.Send(new AccountUserByProviderQuery
        {
            Provider = ProviderName,
            ProviderSubjectId = subject
        });

        if (existingLink != null)
        {
            if (existingLink.AccountId == account.Id)
                return Redirect($"/a/{accountLink}/users?message=oidc_already_linked");

            // Wrong identity picked: end the Keycloak session so a retry can pick another one.
            return OidcSession.SignOutOfProvider(idToken, $"/a/{accountLink}/users?message=oidc_conflict");
        }

        await _mediator.Send(new AddAccountUserCommand
        {
            AccountId = account.Id,
            LoginType = AccountUserLoginType.Oidc,
            Email = providerEmail,
            Provider = ProviderName,
            ProviderSubjectId = subject
        });

        _logger.LogInformation(
            "OIDC account linked for sub {Sub} to account {AccountId} from IP {IpAddress}",
            subject, account.Id, HttpContext.Connection.RemoteIpAddress);

        return Redirect($"/a/{accountLink}/users?message=oidc_linked");
    }

    private async Task<IActionResult> SignInAccount(
        Core.Entities.Account account,
        string subject,
        string loginEmail,
        IReadOnlyList<string> roles,
        string? idToken,
        string? returnUrl,
        IConfiguration configuration)
    {
        var claims = new List<Claim>
        {
            new("sub", account.Uid.ToString()),
            // The person who logged in, not the account's main address: the rest of the app
            // (UserInfo, AdminRequirement, account picker, audit) reads "email" as the login
            // identity, exactly as for an email-code login (AccountCallback).
            new("email", loginEmail),
            new("auth_method", ProviderName),
            new("provider", ProviderName),
            new("provider_sub", subject)
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var configOptions = configuration
            .GetSection(AccountLoginMessageOptions.Location)
            .Get<AccountLoginMessageOptions>()
            ?? throw new Exception("AccountLoginMessageOptions not configured");

        var properties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.Add(configOptions.TokenLifespan)
        };
        // Kept for logout, so it can end the Keycloak session too (AccountCallback).
        OidcSession.StoreIdToken(properties, idToken);

        await HttpContext.SignInAsync(
            IdentityConstants.ApplicationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, IdentityConstants.ApplicationScheme)),
            properties);

        _logger.LogInformation(
            "OIDC login succeeded for email {Email} (account {AccountId}, {AccountEmail}) from IP {IpAddress}",
            loginEmail, account.Id, account.Email, HttpContext.Connection.RemoteIpAddress);
        await _auditService.LogAsync(AuditOutcome.Succeeded,
            target: new AuditTarget { Email = account.Email, AccountUid = account.Uid });

        // returnUrl arrives from the "r" query parameter and round-trips through the
        // provider, so it is attacker-controllable: never redirect off-site with it.
        return Redirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/auto");
    }

    /// <summary>Back to the login page with <paramref name="error"/>, ending the Keycloak session on the way.</summary>
    private IActionResult Reject(string? idToken, string error)
        => OidcSession.SignOutOfProvider(idToken, $"/login?error={Uri.EscapeDataString(error)}");
}

internal record AccountPickerToken
{
    public required string ProviderSub { get; init; }
    public required string Email { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = [];
    public string? IdToken { get; init; }
    public string? ReturnUrl { get; init; }
}

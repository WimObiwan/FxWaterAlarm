using System.Security.Claims;
using System.Text.Json;
using Core.Queries;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Core.Commands;
using Core.Entities;

namespace Site.Pages;

public class GoogleCallback : PageModel
{
    internal const string PickerCookieName = "WaterAlarm.Picker";
    internal const string PickerProtectionPurpose = "AccountPicker.GoogleToken";

    private readonly IMediator _mediator;
    private readonly ILogger<GoogleCallback> _logger;
    private readonly IDataProtectionProvider _dataProtectionProvider;

    public GoogleCallback(IMediator mediator, ILogger<GoogleCallback> logger, IDataProtectionProvider dataProtectionProvider)
    {
        _mediator = mediator;
        _logger = logger;
        _dataProtectionProvider = dataProtectionProvider;
    }

    public async Task<IActionResult> OnGet(
        [FromQuery(Name = "r")] string? returnUrl,
        [FromQuery] string? mode,
        [FromQuery(Name = "a")] string? accountLink,
        [FromServices] IConfiguration configuration)
    {
        var result = await HttpContext.AuthenticateAsync("ExternalCookie");
        if (!result.Succeeded)
        {
            _logger.LogWarning("Google callback: external authentication result missing or invalid from IP {IpAddress}",
                HttpContext.Connection.RemoteIpAddress);
            return RedirectToPage("/Login", new { error = "google_failed" });
        }

        // Consume the external cookie immediately
        await HttpContext.SignOutAsync("ExternalCookie");

        var googleSub = result.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = result.Principal?.FindFirstValue(ClaimTypes.Email);
        var emailVerified = result.Principal?.FindFirst("email_verified")?.Value;

        _logger.LogInformation(
            "Google callback: email={Email}, email_verified={EmailVerified} from IP {IpAddress}",
            email, emailVerified, HttpContext.Connection.RemoteIpAddress);

        // If email_verified claim is missing, assume verified since Google's OAuth requires it
        // If present, it should be "true" (as string)
        if (string.IsNullOrEmpty(email) || (emailVerified != null && emailVerified != "true"))
        {
            _logger.LogWarning(
                "Google login rejected: email not present or not verified (email={Email}, email_verified={EmailVerified})",
                email, emailVerified);
            return RedirectToPage("/Login", new { error = "email_not_verified" });
        }

        if (string.IsNullOrWhiteSpace(googleSub))
        {
            _logger.LogWarning("Google callback: missing sub claim from IP {IpAddress}",
                HttpContext.Connection.RemoteIpAddress);
            return RedirectToPage("/Login", new { error = "google_failed" });
        }

        if (mode == "link" && !string.IsNullOrEmpty(accountLink))
            return await HandleLinkMode(accountLink, googleSub, email!);

        return await HandleLoginMode(returnUrl, googleSub, email!, configuration);
    }

    private async Task<IActionResult> HandleLoginMode(
        string? returnUrl,
        string googleSub,
        string email,
        IConfiguration configuration)
    {
        // Check direct Google link first
        var linkedUser = await _mediator.Send(new AccountUserByProviderQuery
        {
            Provider = "google",
            ProviderSubjectId = googleSub
        });

        if (linkedUser != null)
        {
            var linkedAccount = await _mediator.Send(new AccountByIdQuery { Id = linkedUser.AccountId });
            if (linkedAccount == null)
            {
                _logger.LogWarning("Google login: linked account not found for sub {Sub}", googleSub);
                return RedirectToPage("/Login", new { error = "no_account" });
            }
            return await SignInAccount(linkedAccount, googleSub, returnUrl, configuration);
        }

        // Fall back: look for all mail AccountUsers matching the Google email
        var accounts = await _mediator.Send(new AccountsByEmailQuery { Email = email });

        if (accounts.Count == 0)
        {
            _logger.LogWarning("Google login: no WaterAlarm account found for email {Email} / sub {Sub}", email, googleSub);
            return RedirectToPage("/Login", new { error = "no_account" });
        }

        if (accounts.Count == 1)
        {
            var account = accounts[0];
            // Auto-link this Google identity to the matched account for next logins
            await _mediator.Send(new AddAccountUserCommand
            {
                AccountId = account.Id,
                LoginType = AccountUserLoginType.Google,
                Email = email,
                Provider = "google",
                ProviderSubjectId = googleSub
            });
            return await SignInAccount(account, googleSub, returnUrl, configuration);
        }

        // Multiple accounts match — redirect to picker
        _logger.LogInformation(
            "Google login: multiple accounts ({Count}) found for email {Email}, redirecting to picker",
            accounts.Count, email);

        var token = new AccountPickerToken { GoogleSub = googleSub, Email = email, ReturnUrl = returnUrl };
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
        string googleSub,
        string googleEmail)
    {
        // Must already be signed in
        var session = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);
        if (!session.Succeeded)
            return RedirectToPage("/Login", new { error = "not_authenticated" });

        var account = await _mediator.Send(new AccountByLinkQuery { Link = accountLink });
        if (account == null)
            return RedirectToPage("/Login", new { error = "no_account" });

        // Verify current session user is authorized on this account
        var sessionEmail = session.Principal?.FindFirstValue("email");
        var sessionProvider = session.Principal?.FindFirstValue("provider");
        var sessionProviderSub = session.Principal?.FindFirstValue("provider_sub");

        var accountUsers = await _mediator.Send(new AccountUsersByAccountQuery { AccountId = account.Id });
        var isAuthorized = accountUsers.Any(u =>
            (u.LoginType == AccountUserLoginType.Mail
                && sessionEmail != null
                && string.Equals(u.Email, sessionEmail, StringComparison.OrdinalIgnoreCase))
            || (u.LoginType == AccountUserLoginType.Google
                && sessionProvider == "google"
                && sessionProviderSub != null
                && u.ProviderSubjectId == sessionProviderSub));

        if (!isAuthorized)
            return Forbid();

        // Check if this Google sub is already linked anywhere
        var existingLink = await _mediator.Send(new AccountUserByProviderQuery
        {
            Provider = "google",
            ProviderSubjectId = googleSub
        });

        if (existingLink != null)
        {
            var msg = existingLink.AccountId == account.Id ? "google_already_linked" : "google_conflict";
            return Redirect($"/a/{accountLink}/users?message={msg}");
        }

        await _mediator.Send(new AddAccountUserCommand
        {
            AccountId = account.Id,
            LoginType = AccountUserLoginType.Google,
            Email = googleEmail,
            Provider = "google",
            ProviderSubjectId = googleSub
        });

        _logger.LogInformation(
            "Google account linked for sub {Sub} to account {AccountId} from IP {IpAddress}",
            googleSub, account.Id, HttpContext.Connection.RemoteIpAddress);

        return Redirect($"/a/{accountLink}/users?message=google_linked");
    }

    private async Task<IActionResult> SignInAccount(
        Core.Entities.Account account,
        string googleSub,
        string? returnUrl,
        IConfiguration configuration)
    {
        var claims = new List<Claim>
        {
            new("sub", account.Uid.ToString()),
            new("email", account.Email),
            new("auth_method", "google"),
            new("provider", "google"),
            new("provider_sub", googleSub)
        };

        var configOptions = configuration
            .GetSection(AccountLoginMessageOptions.Location)
            .Get<AccountLoginMessageOptions>()
            ?? throw new Exception("AccountLoginMessageOptions not configured");

        await HttpContext.SignInAsync(
            IdentityConstants.ApplicationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, IdentityConstants.ApplicationScheme)),
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.Add(configOptions.TokenLifespan)
            }
        );

        _logger.LogInformation(
            "Google login succeeded for email {Email} (account {AccountId}) from IP {IpAddress}",
            account.Email, account.Id, HttpContext.Connection.RemoteIpAddress);

        return Redirect(returnUrl ?? "/");
    }
}

internal record AccountPickerToken
{
    public required string GoogleSub { get; init; }
    public required string Email { get; init; }
    public string? ReturnUrl { get; init; }
}

using System.Security.Claims;
using Core.Queries;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Site.Pages;

namespace Site.Pages;

public class GoogleCallback : PageModel
{
    private readonly IMediator _mediator;
    private readonly ILogger<GoogleCallback> _logger;

    public GoogleCallback(IMediator mediator, ILogger<GoogleCallback> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<IActionResult> OnGet(
        [FromQuery(Name = "r")] string? returnUrl,
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

        var account = await _mediator.Send(new AccountByEmailQuery { Email = email });
        if (account == null)
        {
            _logger.LogWarning("Google login: no WaterAlarm account found for email {Email}", email);
            return RedirectToPage("/Login", new { error = "no_account" });
        }

        var claims = new List<Claim>
        {
            new("sub", account.Uid.ToString()),
            new("email", account.Email),
            new("auth_method", "google"),
            new("provider", "google"),
            new("provider_sub", googleSub ?? string.Empty)
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

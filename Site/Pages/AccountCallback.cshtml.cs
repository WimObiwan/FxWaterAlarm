using System.Security.Claims;
using Core.Audit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Site.Pages;

public class AccountCallback : PageModel
{
    internal const string AuditActionLogin = "Auth.Login";
    internal const string AuditActionSignOut = "Auth.SignOut";

    private readonly UserManager<IdentityUser> _userManager;
    private readonly IAuditService _auditService;
    private readonly ILogger<AccountCallback> _logger;
    public string? EmailAddress { get; set; }

    public AccountCallback(UserManager<IdentityUser> userManager, IAuditService auditService,
        ILogger<AccountCallback> logger)
    {
        _userManager = userManager;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<IActionResult> OnGet(string token, string email, string? url,
        [FromServices] IConfiguration configuration)
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
        {
            using var signOutScope = _auditService.BeginAction(AuditActionSignOut,
                new AuditTarget { Email = string.IsNullOrEmpty(email) ? null : email });

            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            _logger.LogInformation("User signed out from IP {IpAddress}", HttpContext.Connection.RemoteIpAddress);
            await _auditService.LogAsync(AuditOutcome.Succeeded);

            if (url == null)
                return Redirect("/");

            return Redirect(Uri.UnescapeDataString(url));
        }

        using var auditScope = _auditService.BeginAction(AuditActionLogin, new AuditTarget { Email = email });
        await _auditService.LogAsync(AuditOutcome.Attempted);

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            _logger.LogWarning("Passwordless login rejected for unknown email {Email} from IP {IpAddress}", email, HttpContext.Connection.RemoteIpAddress);
            await _auditService.LogAsync(AuditOutcome.Denied, new AuditDetails { Reason = "Unknown email address" });
            return RedirectToPage("/Login", new { error = AccountLoginMessage.UnknownEmailError });
        }

        var isValid = await _userManager.VerifyUserTokenAsync(user, "Default", "passwordless-auth", token);

        if (isValid) {
            var claims = new List<Claim>
            {
                new("sub", user.Id),
                new("email", user.Email ?? "")
            };

            AccountLoginMessageOptions accountLoginMessageOptions = configuration
                .GetSection(AccountLoginMessageOptions.Location)
                .Get<AccountLoginMessageOptions>()
            ?? throw new Exception("AccountLoginMessageOptions not configured");

            await HttpContext.SignInAsync(
                IdentityConstants.ApplicationScheme,
                new ClaimsPrincipal(new ClaimsIdentity(claims, IdentityConstants.ApplicationScheme)),
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.Add(accountLoginMessageOptions.TokenLifespan) // Adjust the expiration as needed
                }
            );
            _logger.LogInformation("Passwordless login succeeded for email {Email} from IP {IpAddress}", email, HttpContext.Connection.RemoteIpAddress);
            await _auditService.LogAsync(AuditOutcome.Succeeded);
            if (url == null)
                return Redirect("/auto");

            return Redirect(Uri.UnescapeDataString(url));
        }

        _logger.LogWarning("Passwordless login failed due to invalid token for email {Email} from IP {IpAddress}", email, HttpContext.Connection.RemoteIpAddress);
        await _auditService.LogAsync(AuditOutcome.Failed, new AuditDetails { Reason = "Invalid token" });

        return Redirect("Error");
    }
}

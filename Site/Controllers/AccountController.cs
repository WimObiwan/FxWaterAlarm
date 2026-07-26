using System.Security.Claims;
using Core.Audit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Site.Pages;

namespace Site.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IAuditService _auditService;

    public AccountController(UserManager<IdentityUser> userManager, IAuditService auditService)
    {
        _userManager = userManager;
        _auditService = auditService;
    }

    public async Task<IActionResult> LoginCallback(string token, string email,
        [FromServices] IConfiguration configuration)
    {
        using var auditScope = _auditService.BeginAction(AccountCallback.AuditActionLogin,
            new AuditTarget { Email = email });
        await _auditService.LogAsync(AuditOutcome.Attempted);

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            await _auditService.LogAsync(AuditOutcome.Denied, new AuditDetails { Reason = "Unknown email address" });
            return RedirectToPage("/Login", new { error = AccountLoginMessage.UnknownEmailError });
        }

        var isValid = await _userManager.VerifyUserTokenAsync(user, "Default", "passwordless-auth", token);

        if (isValid) {
            await _userManager.UpdateSecurityStampAsync(user);

            AccountLoginMessageOptions accountLoginMessageOptions =
            configuration
                .GetSection(AccountLoginMessageOptions.Location)
                .Get<AccountLoginMessageOptions>()
            ?? throw new Exception("AccountLoginMessageOptions not configured");

            await HttpContext.SignInAsync(
                IdentityConstants.ApplicationScheme,
                new ClaimsPrincipal(new ClaimsIdentity(
                    new List<Claim> { new Claim("sub", user.Id) },
                    IdentityConstants.ApplicationScheme)),
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.Add(accountLoginMessageOptions.TokenLifespan) // Adjust the expiration
                }
            );
            await _auditService.LogAsync(AuditOutcome.Succeeded);
            return Redirect("/auto");
        }

        await _auditService.LogAsync(AuditOutcome.Failed, new AuditDetails { Reason = "Invalid token" });
        return View("Error");
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Site.Pages;

namespace Site.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;

    public AccountController(UserManager<IdentityUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IActionResult> LoginCallback(string token, string email,
        [FromServices] IConfiguration configuration)
    {
        var user = await _userManager.FindByEmailAsync(email) ?? throw new Exception("User not found");
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
            return Redirect("/");
        }

        return View("Error");
    }
}
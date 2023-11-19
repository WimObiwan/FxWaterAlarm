using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Site.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;

    public AccountController(UserManager<IdentityUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IActionResult> LoginCallback(string token, string email)
    {
        var user = await _userManager.FindByEmailAsync(email) ?? throw new Exception("User not found");
        var isValid = await _userManager.VerifyUserTokenAsync(user, "Default", "passwordless-auth", token);

        if (isValid) {
            await _userManager.UpdateSecurityStampAsync(user);

            await HttpContext.SignInAsync(
                IdentityConstants.ApplicationScheme,
                new ClaimsPrincipal(new ClaimsIdentity(
                    new List<Claim> {new Claim("sub", user.Id)},
            IdentityConstants.ApplicationScheme)));
            return Redirect("/");
        }

        return View("Error");
    }
}
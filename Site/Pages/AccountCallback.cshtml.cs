using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Site.Pages;

public class AccountCallback : PageModel
{
    private readonly UserManager<IdentityUser> _userManager;
    public string? EmailAddress { get; set; }

    public AccountCallback(UserManager<IdentityUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IActionResult> OnGet(string token, string email, string? url,
        [FromServices] IConfiguration configuration)
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
        {
            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            if (url == null)
                return Redirect("/");

            return Redirect(Uri.UnescapeDataString(url));
        }

        var user = await _userManager.FindByEmailAsync(email) ?? throw new Exception("User not found");
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
            if (url == null)
                return Redirect("/");
            
            return Redirect(Uri.UnescapeDataString(url));
        }

        return Redirect("Error");
    }
}
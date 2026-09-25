using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using Site.Authentication;

namespace Site.Pages;

public class Login : PageModel
{
    public string? ReturnUrl { get; set; }
    public string? Error { get; set; }
    public bool OidcEnabled { get; private set; }

    public IActionResult OnGet(
        [FromQuery(Name = "r")] string? returnUrl = null,
        [FromQuery] string? error = null,
        [FromServices] IOptionsSnapshot<OidcOptions>? oidcOptions = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return Redirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/auto");

        ReturnUrl = returnUrl;
        Error = error;
        OidcEnabled = oidcOptions?.Value.IsConfigured ?? false;
        return Page();
    }

    public IActionResult OnGetOidc(
        [FromQuery(Name = "r")] string? returnUrl = null,
        [FromServices] IOptionsSnapshot<OidcOptions>? oidcOptions = null)
    {
        if (!(oidcOptions?.Value.IsConfigured ?? false))
            return RedirectToPage("/Login", new { r = returnUrl, error = "oidc_not_configured" });

        // Drop a non-local returnUrl here rather than at the callback: it round-trips
        // through the provider in the auth properties and must not be attacker-chosen.
        var safeReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null;

        var callbackUrl = Url.Page("/OidcCallback", values: new { r = safeReturnUrl });
        var properties = new AuthenticationProperties { RedirectUri = callbackUrl };
        return Challenge(properties, "oidc");
    }
}

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
    public bool GoogleEnabled { get; private set; }

    public IActionResult OnGet(
        [FromQuery(Name = "r")] string? returnUrl = null,
        [FromQuery] string? error = null,
        [FromServices] IOptionsSnapshot<GoogleAuthOptions>? googleOptions = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return Redirect(returnUrl ?? "/auto");

        ReturnUrl = returnUrl;
        Error = error;
        GoogleEnabled = googleOptions?.Value.IsConfigured ?? false;
        return Page();
    }

    public IActionResult OnGetGoogle(
        [FromQuery(Name = "r")] string? returnUrl = null,
        [FromServices] IOptionsSnapshot<GoogleAuthOptions>? googleOptions = null)
    {
        if (!(googleOptions?.Value.IsConfigured ?? false))
            return RedirectToPage("/Login", new { r = returnUrl, error = "google_not_configured" });

        var callbackUrl = Url.Page("/GoogleCallback", values: new { r = returnUrl });
        var properties = new AuthenticationProperties { RedirectUri = callbackUrl };
        return Challenge(properties, "Google");
    }
}

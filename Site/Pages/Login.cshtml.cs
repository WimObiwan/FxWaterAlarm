using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Site.Pages;

public class Login : PageModel
{
    public string? ReturnUrl { get; set; }

    public IActionResult OnGet([FromQuery(Name = "r")] string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return Redirect(returnUrl ?? "/");

        ReturnUrl = returnUrl;
        return Page();
    }
}

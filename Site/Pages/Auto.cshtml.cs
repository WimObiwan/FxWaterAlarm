using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Site.Pages;

public class Auto : PageModel
{
    public IActionResult OnGet()
    {
        string? url = HttpContext.Request.Cookies["auto"];

        if (string.IsNullOrEmpty(url))
            return Redirect("/");
                
        return Redirect(url);
    }
}
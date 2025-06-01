using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Site.Pages;

public class Photos : PageModel
{
    public IActionResult OnGet()
    {
        return Redirect("https://photos.app.goo.gl/2KgbPVx412SULxkK9");
    }
}
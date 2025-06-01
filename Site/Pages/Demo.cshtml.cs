using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Site.Pages;

public class Demo : PageModel
{
    public IActionResult OnGet()
    {
        // Buiten
        return Redirect($"/a/demo/s/f2y616afaEA");
        // Garage
        // return Redirect($"/a/demo/s/ter0TfgBG58");
    }
}
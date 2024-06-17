using Core.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Site.Pages;

public class Demo : PageModel
{
    public IActionResult OnGet()
    {
        // Buiten
        // return Redirect($"/a/demo/s/i5WOmUdoO0");
        // Garage
        return Redirect($"/a/demo/s/ter0TfgBG58");
    }
}
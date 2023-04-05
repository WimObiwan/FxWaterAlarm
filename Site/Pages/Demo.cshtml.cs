using Core.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Site.Pages;

public class Demo : PageModel
{
    public async Task<IActionResult> OnGet()
    {
        return Redirect($"/a/demo/s/i5WOmUdoO0");
    }
}
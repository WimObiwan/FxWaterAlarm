using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Site.Pages;

public class Demo : PageModel
{
    private readonly IConfiguration _configuration;

    public Demo(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IActionResult OnGet()
    {
        return Redirect(_configuration["DemoPath"] ?? "/a/demo/s/f2y616afaEA");

        // Buiten
        //return Redirect("/a/demo/s/f2y616afaEA");
        // Garage
        //return Redirect($"/a/demo/s/ter0TfgBG58");
    }
}
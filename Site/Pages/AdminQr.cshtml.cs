using Core.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Site.Pages;

[Authorize(Policy = "Admin")]
public class AdminQr : PageModel
{
    public string QrUrl {get; set; } = string.Empty;

    public AdminQr()
    {
    }

    public void OnGet([FromQuery] string url)
    {
        QrUrl = url;
    }
}
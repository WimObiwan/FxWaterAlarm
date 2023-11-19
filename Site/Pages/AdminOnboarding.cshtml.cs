using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Site.Pages;

[Authorize(Policy = "Admin")]
public class AdminOnBoarding : PageModel
{
    public void OnGet()
    {
    }
}
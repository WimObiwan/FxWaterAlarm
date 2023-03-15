using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Site.Pages;

[AllowAnonymous]
public class Info : PageModel
{
    public string? RequestHost { get; set; }
    public string? Server { get; set; }
    public string? Version { get; set; }
    public string? OsVersion { get; set; }
    public string? DotNetVersion { get; set; }
    public string? UserAuthId { get; set; }

    public Info()
    {
    }
    
    public void OnGet()
    {
        RequestHost = HttpContext.Request.Host.ToString();
        Server = Environment.MachineName;
        Version = Assembly.GetEntryAssembly()
            ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        OsVersion = RuntimeInformation.OSDescription;
        DotNetVersion = RuntimeInformation.FrameworkDescription;
        UserAuthId = HttpContext.User.Identity?.Name;
    }
}
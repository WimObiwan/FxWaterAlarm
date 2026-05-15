using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace Site.Pages;

[AllowAnonymous]
public class Info : PageModel
{
    private readonly IConfiguration _config;

    public Info(IConfiguration config)
    {
        _config = config;
    }

    public string? RequestHost { get; set; }
    public string? Server { get; set; }
    public string? Version { get; set; }
    public string? OsVersion { get; set; }
    public string? DotNetVersion { get; set; }
    public string? UserAuthId { get; set; }
    public string? UserAgent { get; set; }
    public bool IsAuthenticated { get; set; }
    public string? UserEmail { get; set; }
    public string? GoogleId { get; set; }
    public string? LoginMethod { get; set; }
    public DateTimeOffset? LoginTimestamp { get; set; }
    public DateTimeOffset? SessionExpiresAt { get; set; }
    public bool IsAdmin { get; set; }
    public string? ClientIpAddress { get; set; }
    public string? ClientForwardedFor { get; set; }
    public string? UserClaimsSummary { get; set; }
    public string? TroubleshootingInfo { get; set; }
    public string? AutoCookieLink { get; set; }
    public bool UseDevBranding { get; set; }

    public void OnGet()
    {
        RequestHost = HttpContext.Request.Host.ToString();
        Server = Environment.MachineName;
        Version = ThisAssembly.Git.SemVer.Major + "." + ThisAssembly.Git.SemVer.Minor + "." + ThisAssembly.Git.Commits 
                  + "-" + ThisAssembly.Git.Branch + "+" + ThisAssembly.Git.Commit
                  + " " + ThisAssembly.Git.CommitDate;
        OsVersion = RuntimeInformation.OSDescription;
        DotNetVersion = RuntimeInformation.FrameworkDescription;
        UserAgent = HttpContext.Request.Headers.UserAgent;
        UseDevBranding = _config.GetValue<bool>("UiBranding:UseDevBranding");
        AutoCookieLink = HttpContext.Request.Cookies["auto"];

        IsAuthenticated = HttpContext.User.Identity?.IsAuthenticated ?? false;
        UserEmail = HttpContext.User.FindFirstValue("email");
        GoogleId = HttpContext.User.FindFirstValue("provider_sub");

        var provider = HttpContext.User.FindFirstValue("provider");
        LoginMethod = provider == "google" || !string.IsNullOrEmpty(GoogleId)
            ? "google"
            : (IsAuthenticated ? "email" : null);

        var services = HttpContext.RequestServices;

        var authenticationService = services?.GetService(typeof(IAuthenticationService)) as IAuthenticationService;
        if (authenticationService != null)
        {
            var authResult = HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme).GetAwaiter().GetResult();
            if (authResult.Succeeded)
            {
                LoginTimestamp = authResult.Properties?.IssuedUtc;
                SessionExpiresAt = authResult.Properties?.ExpiresUtc;
            }
        }

        var authorizationService = services?.GetService(typeof(IAuthorizationService)) as IAuthorizationService;
        IsAdmin = authorizationService
            ?.AuthorizeAsync(HttpContext.User, "Admin")
            .GetAwaiter()
            .GetResult()
            .Succeeded ?? false;
        ClientIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        ClientForwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].ToString();
        UserClaimsSummary = string.Join(", ",
            HttpContext.User.Claims
                .Select(c => c.Type)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(x => x, StringComparer.Ordinal));

        TroubleshootingInfo = BuildTroubleshootingInfo();
    }

    private string BuildTroubleshootingInfo()
    {
        string FormatTimestamp(DateTimeOffset? ts) => ts?.ToString("yyyy-MM-dd HH:mm:ss zzz") ?? "unknown";
        string Safe(string? value) => string.IsNullOrWhiteSpace(value) ? "-" : value;

        return string.Join('\n',
        [
            "WaterAlarm /info troubleshooting",
            $"RequestHost: {Safe(RequestHost)}",
            $"Server: {Safe(Server)}",
            $"Version: {Safe(Version)}",
            $"OsVersion: {Safe(OsVersion)}",
            $"DotNetVersion: {Safe(DotNetVersion)}",
            $"Environment: {Safe(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"))}",
            $"DateTimeLocal: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            $"DateTimeUtc: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC",
            $"Culture: {CultureInfo.CurrentCulture.Name}",
            $"UICulture: {CultureInfo.CurrentUICulture.Name}",
            $"IsAuthenticated: {IsAuthenticated}",
            $"LoginMethod: {Safe(LoginMethod)}",
            $"UserEmail: {Safe(UserEmail)}",
            $"GoogleId: {Safe(GoogleId)}",
            $"LoginTimestamp: {FormatTimestamp(LoginTimestamp)}",
            $"SessionExpiresAt: {FormatTimestamp(SessionExpiresAt)}",
            $"IsAdmin: {IsAdmin}",
            $"ClientIpAddress: {Safe(ClientIpAddress)}",
            $"XForwardedFor: {Safe(ClientForwardedFor)}",
            $"UserAgent: {Safe(UserAgent)}",
            $"ClaimTypes: {Safe(UserClaimsSummary)}",
            $"UseDevBranding: {UseDevBranding}",
            $"AutoCookieLink: {Safe(AutoCookieLink)}"
        ]);
    }
}
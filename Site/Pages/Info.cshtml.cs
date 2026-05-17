using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Globalization;
using System.IO;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Site.Pages;

[AllowAnonymous]
public class Info : PageModel
{
    private readonly IConfiguration _config;
    private readonly IOptionsMonitor<CookieAuthenticationOptions> _cookieAuthenticationOptions;

    public Info(IConfiguration config, IOptionsMonitor<CookieAuthenticationOptions> cookieAuthenticationOptions)
    {
        _config = config;
        _cookieAuthenticationOptions = cookieAuthenticationOptions;
    }

    public string? RequestHost { get; set; }
    public string? RequestScheme { get; set; }
    public bool RequestIsHttps { get; set; }
    public string? Server { get; set; }
    public string? Version { get; set; }
    public DateTimeOffset? BuildDateTime { get; set; }
    public string? BuildDateTimeSource { get; set; }
    public string? OsVersion { get; set; }
    public string? DotNetVersion { get; set; }
    public string? UserAuthId { get; set; }
    public DateTimeOffset? ProcessStartTime { get; set; }
    public string? ConfiguredTokenLifespan { get; set; }
    public string? ConfiguredCookieExpireTimeSpan { get; set; }
    public bool CookieSlidingExpiration { get; set; }
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
    public string? ClientForwardedProto { get; set; }
    public bool HasAuthCookie { get; set; }
    public int? AuthCookieLength { get; set; }
    public string? AuthTicketStatus { get; set; }
    public string? AuthTicketFailure { get; set; }
    public string? SessionAge { get; set; }
    public string? SessionRemaining { get; set; }
    public string? UserClaimsSummary { get; set; }
    public string? TroubleshootingInfo { get; set; }
    public string? AutoCookieLink { get; set; }
    public bool UseDevBranding { get; set; }

    public void OnGet()
    {
        RequestHost = HttpContext.Request.Host.ToString();
        RequestScheme = HttpContext.Request.Scheme;
        RequestIsHttps = HttpContext.Request.IsHttps;
        Server = Environment.MachineName;
        Version = ThisAssembly.Git.SemVer.Major + "." + ThisAssembly.Git.SemVer.Minor + "." + ThisAssembly.Git.Commits 
                  + "-" + ThisAssembly.Git.Branch + "+" + ThisAssembly.Git.Commit
                  + " " + ThisAssembly.Git.CommitDate;
        (BuildDateTime, BuildDateTimeSource) = ResolveBuildOrDeployDateTime();
        OsVersion = RuntimeInformation.OSDescription;
        DotNetVersion = RuntimeInformation.FrameworkDescription;
        try
        {
            ProcessStartTime = System.Diagnostics.Process.GetCurrentProcess().StartTime;
        }
        catch
        {
            ProcessStartTime = null;
        }
        UserAgent = HttpContext.Request.Headers.UserAgent;
        UseDevBranding = _config.GetValue<bool>("UiBranding:UseDevBranding");
        AutoCookieLink = HttpContext.Request.Cookies["auto"];
        var authCookieValue = HttpContext.Request.Cookies["WaterAlarm.Auth"];
        HasAuthCookie = !string.IsNullOrEmpty(authCookieValue);
        AuthCookieLength = authCookieValue?.Length;

        var accountLoginMessageOptions = _config
            .GetSection(AccountLoginMessageOptions.Location)
            .Get<AccountLoginMessageOptions>();
        ConfiguredTokenLifespan = accountLoginMessageOptions?.TokenLifespan.ToString();

        var cookieOptions = _cookieAuthenticationOptions.Get(IdentityConstants.ApplicationScheme);
        ConfiguredCookieExpireTimeSpan = cookieOptions.ExpireTimeSpan.ToString();
        CookieSlidingExpiration = cookieOptions.SlidingExpiration;

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
            AuthTicketStatus = authResult.Succeeded
                ? "Succeeded"
                : authResult.None
                    ? "None"
                    : "Failed";
            AuthTicketFailure = authResult.Failure == null
                ? null
                : $"{authResult.Failure.GetType().Name}: {authResult.Failure.Message}";
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
        ClientForwardedProto = HttpContext.Request.Headers["X-Forwarded-Proto"].ToString();
        UserClaimsSummary = string.Join(", ",
            HttpContext.User.Claims
                .Select(c => c.Type)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(x => x, StringComparer.Ordinal));

        if (LoginTimestamp.HasValue)
            SessionAge = (DateTimeOffset.UtcNow - LoginTimestamp.Value).ToString();
        if (SessionExpiresAt.HasValue)
            SessionRemaining = (SessionExpiresAt.Value - DateTimeOffset.UtcNow).ToString();

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
            $"RequestScheme: {Safe(RequestScheme)}",
            $"RequestIsHttps: {RequestIsHttps}",
            $"Server: {Safe(Server)}",
            $"Version: {Safe(Version)}",
            $"BuildDateTime: {FormatTimestamp(BuildDateTime)}",
            $"BuildDateTimeSource: {Safe(BuildDateTimeSource)}",
            $"OsVersion: {Safe(OsVersion)}",
            $"DotNetVersion: {Safe(DotNetVersion)}",
            $"ProcessStartTime: {FormatTimestamp(ProcessStartTime)}",
            $"Environment: {Safe(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"))}",
            $"DateTimeLocal: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            $"DateTimeUtc: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC",
            $"Culture: {CultureInfo.CurrentCulture.Name}",
            $"UICulture: {CultureInfo.CurrentUICulture.Name}",
            $"ConfiguredTokenLifespan: {Safe(ConfiguredTokenLifespan)}",
            $"ConfiguredCookieExpireTimeSpan: {Safe(ConfiguredCookieExpireTimeSpan)}",
            $"CookieSlidingExpiration: {CookieSlidingExpiration}",
            $"HasAuthCookie: {HasAuthCookie}",
            $"AuthCookieLength: {AuthCookieLength?.ToString() ?? "-"}",
            $"IsAuthenticated: {IsAuthenticated}",
            $"AuthTicketStatus: {Safe(AuthTicketStatus)}",
            $"AuthTicketFailure: {Safe(AuthTicketFailure)}",
            $"LoginMethod: {Safe(LoginMethod)}",
            $"UserEmail: {Safe(UserEmail)}",
            $"GoogleId: {Safe(GoogleId)}",
            $"LoginTimestamp: {FormatTimestamp(LoginTimestamp)}",
            $"SessionExpiresAt: {FormatTimestamp(SessionExpiresAt)}",
            $"SessionAge: {Safe(SessionAge)}",
            $"SessionRemaining: {Safe(SessionRemaining)}",
            $"IsAdmin: {IsAdmin}",
            $"ClientIpAddress: {Safe(ClientIpAddress)}",
            $"XForwardedFor: {Safe(ClientForwardedFor)}",
            $"XForwardedProto: {Safe(ClientForwardedProto)}",
            $"UserAgent: {Safe(UserAgent)}",
            $"ClaimTypes: {Safe(UserClaimsSummary)}",
            $"UseDevBranding: {UseDevBranding}",
            $"AutoCookieLink: {Safe(AutoCookieLink)}"
        ]);
    }

    private (DateTimeOffset? Value, string? Source) ResolveBuildOrDeployDateTime()
    {
        var deployStampPath = _config["Deployment:StampFilePath"]
                             ?? Path.Combine(AppContext.BaseDirectory, ".wateralarm-deploy-time");
        if (System.IO.File.Exists(deployStampPath))
        {
            try
            {
            var raw = System.IO.File.ReadAllText(deployStampPath).Trim();
                if (DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var stamp))
                    return (stamp, $"deploy stamp ({deployStampPath})");
            }
            catch
            {
                // Fall back to assembly timestamp when deploy stamp cannot be read.
            }
        }

        var entryAssemblyLocation = Assembly.GetEntryAssembly()?.Location;
        if (!string.IsNullOrWhiteSpace(entryAssemblyLocation) && System.IO.File.Exists(entryAssemblyLocation))
        {
            var lastWrite = System.IO.File.GetLastWriteTime(entryAssemblyLocation);
            if (lastWrite != default)
                return (new DateTimeOffset(lastWrite), $"assembly last write ({Path.GetFileName(entryAssemblyLocation)})");
        }

        return (null, null);
    }
}
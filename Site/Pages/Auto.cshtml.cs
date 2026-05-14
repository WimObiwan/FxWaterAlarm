using System.Security.Claims;
using System.Text.RegularExpressions;
using Core.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Site.Utilities;
using CookieOptions = Microsoft.AspNetCore.Http.CookieOptions;

namespace Site.Pages;

public class Auto : PageModel
{
    private readonly IMediator _mediator;
    private readonly IUserInfo _userInfo;
    public string? Link { get; set; } = null;

    public Auto(IMediator mediator, IUserInfo userInfo)
    {
        _mediator = mediator;
        _userInfo = userInfo;
    }
    
    public async Task<IActionResult> OnGet(bool update = false)
    {
        string? url = HttpContext.Request.Cookies["auto"];

        if (!update && !string.IsNullOrEmpty(url))
        {
            var testedUrl = await TestAutoLink(url);

            if (testedUrl != null)
            {
                if (testedUrl != url)
                {
                    Response.Cookies.Append("auto", testedUrl, new CookieOptions { MaxAge = TimeSpan.FromDays(365 * 10) });
                }
                
                return Redirect(testedUrl);
            }
        }

        // No valid link cookie — if authenticated, resolve account from claims
        if (!update && User.Identity?.IsAuthenticated == true)
        {
            var subClaim = User.FindFirstValue("sub");
            if (Guid.TryParse(subClaim, out var uid))
            {
                var account = await _mediator.Send(new AccountByUidQuery { Uid = uid });
                var appPath = await GetAppPath(account);
                if (appPath != null)
                    return Redirect(appPath);
            }

            var email = User.FindFirstValue("email");
            if (!string.IsNullOrEmpty(email))
            {
                var accounts = await _mediator.Send(new AccountsByEmailQuery { Email = email }) ?? [];

                if (accounts.Count > 1)
                    return Redirect("/account-picker");

                if (accounts.Count == 1)
                {
                    var appPath = await GetAppPath(accounts[0]);
                    if (appPath != null)
                        return Redirect(appPath);
                }
            }

            // Authenticated admin without account context should land on admin dashboard.
            if (await _userInfo.IsAdmin())
                return Redirect("/adm");
        }

        Link = url;
        return Page();
    }

    public IActionResult OnPost(string link)
    {
        if (string.IsNullOrEmpty(link))
        {
            Response.Cookies.Delete("auto");
        }
        else
        {
            Response.Cookies.Append("auto", link, new CookieOptions { MaxAge = TimeSpan.FromDays(365 * 10) });
        }

        return Redirect("/auto");
    }

    private async Task<string?> TestAutoLink(string link)
    {
        bool found;

        Regex regex = new(".*/a/([^/]+)(?:/s/([^/]+))?");
        var match = regex.Match(link);
        if (match.Success)
        {
            var accountLink = match.Groups[1].Value;
            var sensorLink = match.Groups[2].Success ? match.Groups[2].Value : null;
            if (sensorLink != null)
            {
                var result = await _mediator.Send(new AccountSensorByLinkQuery
                {
                    SensorLink = sensorLink,
                    AccountLink = accountLink
                });
                found = result != null;
            }
            else
            {
                var result = await _mediator.Send(new AccountByLinkQuery
                {
                    Link = accountLink
                });
                found = result != null;
            }
            
            if (found)
            {
                if (sensorLink != null)
                    return $"/a/{accountLink}/s/{sensorLink}";
                else
                    return $"/a/{accountLink}";
            }
        }

        return null;
    }

    private async Task<string?> GetAppPath(Core.Entities.Account? account)
    {
        if (account?.Link == null)
            return null;

        var accountWithSensors = await _mediator.Send(new AccountByLinkQuery { Link = account.Link });
        return accountWithSensors?.AppPath;
    }
}
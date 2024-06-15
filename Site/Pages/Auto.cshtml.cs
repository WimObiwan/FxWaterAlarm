using System.Text.RegularExpressions;
using Core.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using CookieOptions = Microsoft.AspNetCore.Http.CookieOptions;

namespace Site.Pages;

public class Auto : PageModel
{
    private readonly IMediator _mediator;
    public string? Link { get; set; } = null;

    public Auto(IMediator mediator)
    {
        _mediator = mediator;
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
}
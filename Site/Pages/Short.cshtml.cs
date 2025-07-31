using Core.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Site.Pages;

public class Short : PageModel
{
    public Short()
    {
    }

    public IActionResult OnGet([FromQuery(Name = "c")] string? context)
    {
        string? queryString;
        if (context == null)
        {
            queryString = null;
        }
        else
        {
            var parts = context.Split('|');
            if (parts.Length < 3)
            {
                // Invalid context format
                queryString = null;
            }
            else
            {
                var queryStringBuilder = System.Web.HttpUtility.ParseQueryString(string.Empty);
                queryStringBuilder.Add("utm_source", parts[0]);
                queryStringBuilder.Add("utm_medium", parts[1]);
                queryStringBuilder.Add("utm_campaign", parts[2]);
                if (parts.Length >= 4)
                    queryStringBuilder.Add("utm_id", parts[3]);

                queryString = queryStringBuilder?.ToString();
            }
        }

        const string blogUrl = "https://blog.wateralarm.be";

        if (queryString != null)
        {
            // e.g. https://blog.wateralarm.be?utm_source=qr&utm_medium=pst&utm_campaign=th25&utm_id=111
            return Redirect($"{blogUrl}?{queryString}");
        }
        else
        {
            return Redirect(blogUrl);
        }
    }
}
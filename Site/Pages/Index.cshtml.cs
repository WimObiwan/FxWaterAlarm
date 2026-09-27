using Microsoft.AspNetCore.Mvc.RazorPages;
using Site.Utilities;

namespace Site.Pages;

public class IndexModel : PageModel
{
    private readonly IUserInfo _userInfo;

    public IndexModel(IUserInfo userInfo)
    {
        _userInfo = userInfo;
    }

    // A returning customer: logged in, or /auto has remembered their account/sensor link.
    public bool HasAutoLink { get; private set; }

    public void OnGet()
    {
        HasAutoLink = _userInfo.IsAuthenticated()
                      || !string.IsNullOrEmpty(Request.Cookies["auto"]);
    }
}

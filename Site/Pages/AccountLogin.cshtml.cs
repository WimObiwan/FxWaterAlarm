using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Site.Pages;

public class AccountLogin : PageModel
{
    private readonly UserManager<IdentityUser> _userManager;
    public string? EmailAddress { get; set; }

    public AccountLogin(UserManager<IdentityUser> userManager)
    {
        _userManager = userManager;
    }
    
    public void OnGet(string accountLink)
    {
    }
    
    public async Task<IActionResult> OnPost(string emailAddress, string returnUrl)
    {
        var user = await _userManager.FindByEmailAsync(emailAddress) ?? throw new Exception("User not found");
        string token = await _userManager.GenerateUserTokenAsync(user, "Default", "passwordless-auth");
        // var url = Url.Action("LoginCallback", "Account", new {token = token, email = emailAddress}, Request.Scheme);
        var url = Url.PageLink("AccountCallback", pageHandler: null, 
            values: new { token = token, email = emailAddress, url = returnUrl },
            protocol: Request.Scheme);
        Debug.WriteLine(url);

        var smtpClient = new SmtpClient("uit.telenet.be")
        {
            Port = 587,
            Credentials = new NetworkCredential("wim-devos@telenet.be", "vyX5UttL"),
            EnableSsl = true,
        };
    
        smtpClient.Send("wim-devos@telenet.be", "wim@obiwan.be", "subject", url);
        
        return Redirect("/Account/LoginMailConfirmation");
    } 
}
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using Core.Exceptions;
using Core.Queries;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Options;

namespace Site.Pages;

public class AccountLoginMail : PageModel
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IMediator _mediator;
    private readonly AuthenticationMailOptions _authenticationMailOptions;
    public string? EmailAddress;

    public AccountLoginMail(UserManager<IdentityUser> userManager, IMediator mediator, 
        IOptions<AuthenticationMailOptions> authenticationMailOptions)
    {
        _userManager = userManager;
        _mediator = mediator;
        _authenticationMailOptions = authenticationMailOptions.Value;
    }
    
    public async Task<ActionResult> OnGet(
        [FromQuery(Name = "m")] int mode = 1, 
        [FromQuery(Name = "a")] string? accountLink = null, 
        [FromQuery(Name = "r")] string? returnUrl = null,
        [FromQuery(Name = "e")] string? emailAddress = null)
    {
        switch (mode)
        {
            case 1:
                if (accountLink != null)
                {
                    var result = await SendMailToAccountLink(accountLink, returnUrl);
                    EmailAddress = result.EmailAddress;
                }
                else
                    return BadRequest();
                break;
            case 11:
                EmailAddress = emailAddress;
                break;
        }

        return Page();
    }

    public async Task<IActionResult> OnPost(string emailAddress, string returnUrl)
    {
        var result = await SendMail(emailAddress, returnUrl);
        
        return Redirect($"/Account/LoginMail?m=11&e={result.EmailAddress}");
    }
    
    class SendMailResult
    {
        public required string EmailAddress { get; init; } 
    }
    
    private async Task<SendMailResult> SendMail(string emailAddress, string? returnUrl)
    {
        string? escapedReturnUrl;
        if (returnUrl == null)
            escapedReturnUrl = null;
        else
            escapedReturnUrl = Uri.EscapeDataString(returnUrl);
        
        var user = await _userManager.FindByEmailAsync(emailAddress) ?? throw new Exception("User not found");
        string token = await _userManager.GenerateUserTokenAsync(user, "Default", "passwordless-auth");
        // var url = Url.Action("LoginCallback", "Account", new {token = token, email = emailAddress}, Request.Scheme);
        var url = Url.PageLink("AccountCallback", pageHandler: null, 
            values: new { token = token, email = emailAddress, url = escapedReturnUrl },
            protocol: Request.Scheme);
        Debug.WriteLine(url);

        var smtpClient = new SmtpClient(_authenticationMailOptions.SmtpServer)
        {
            Port = 587,
            Credentials = new NetworkCredential(_authenticationMailOptions.SmtpUsername,
                _authenticationMailOptions.SmtpPassword),
            EnableSsl = true,
        };
    
        smtpClient.Send(_authenticationMailOptions.Sender, emailAddress, 
            "WaterAlarm.be e-mail verificatie", url);

        return new SendMailResult
        {
            EmailAddress = emailAddress
        };
    }

    private async Task<SendMailResult> SendMailToAccountLink(string accountLink, string? returnUrl)
    {
        var account = await _mediator.Send(new AccountByLinkQuery
        {
            Link = accountLink
        });

        if (account == null)
        {
            throw new AccountNotFoundException();
        }

        var email = account.Email;
        return await SendMail(email, returnUrl);
    }
}
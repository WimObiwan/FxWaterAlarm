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
            EmailAddress = AnonymizeEmail(emailAddress)
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
    
    static string AnonymizeEmail(string email)
    {
        // Split the email into local part and domain
        string[] parts = email.Split('@');
        
        // Anonymize the local part (e.g., "example" becomes "ex***le")
        string anonymizedLocalPart = AnonymizeString(parts[0]);

        // Split the domain into subdomains and the top-level domain
        string[] domainParts = parts[1].Split('.');
        int domainPartsCount = domainParts.Length;

        // Anonymize the domain, except for the last part
        for (int i = 0; i < domainPartsCount - 1; i++)
        {
            domainParts[i] = AnonymizeString(domainParts[i]);
        }

        // Combine the anonymized local part, "@" symbol, and the reconstructed domain
        return anonymizedLocalPart + "@" + string.Join(".", domainParts);
    }

    static string AnonymizeString(string input)
    {
        if (input.Length <= 2)
        {
            // If the string has 2 or fewer characters, keep the first character and mask the rest
            return input.Substring(0, 1) + new string('*', input.Length - 1);
        }
        else
        {
            // Keep the first and last characters, and mask the characters in between
            return input.Substring(0, 1) + new string('*', input.Length - 2) + input.Substring(input.Length - 1, 1);
        }
    }
}
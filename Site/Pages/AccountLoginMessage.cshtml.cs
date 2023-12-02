using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using Core.Exceptions;
using Core.Queries;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Options;
using QRCoder;
using Site.Communication;

namespace Site.Pages;

public class AccountLoginMessageOptions
{
    public const string Location = "AccountLoginMessage";

    public required TimeSpan? TokenLifespan { get; init; }
    public required string Salt { get; init; }
}

public class AccountLoginMessage : PageModel
{
    private static Random _random = new Random();

    private readonly UserManager<IdentityUser> _userManager;
    private readonly IMediator _mediator;
    private readonly IMessenger _messenger;
    private readonly AccountLoginMessageOptions _options;
    public string? EmailAddress { get; set; }
    public string? AccountLink { get; set; }
    public string? ReturnUrl { get; set; }
    public string? Cookie { get; set; }
    public string? ResendMailUrl { get; set; }
    public bool WrongCode { get; set; }

    public AccountLoginMessage(UserManager<IdentityUser> userManager, IMediator mediator, 
        IMessenger messenger,
        IOptions<AccountLoginMessageOptions> options)
    {
        _userManager = userManager;
        _mediator = mediator;
        _messenger = messenger;
        _options = options.Value;
    }
    
    public async Task<ActionResult> OnGet(
        [FromQuery(Name = "m")] int mode = 1, 
        [FromQuery(Name = "a")] string? accountLink = null, 
        [FromQuery(Name = "r")] string? returnUrl = null,
        [FromQuery(Name = "e")] string? emailAddress = null,
        [FromQuery(Name = "c")] string? cookie = null)
    {
        switch (mode)
        {
            case 1:
                if (accountLink != null)
                {
                    var result = await SendMailToAccountLink(accountLink, returnUrl);
                    EmailAddress = result.EmailAddress;
                    AccountLink = accountLink;
                    Cookie = result.Cookie;
                    ReturnUrl = returnUrl;
                }
                else
                    return BadRequest();
                break;
            case 11:
            case 12:
                EmailAddress = emailAddress;
                AccountLink = accountLink;
                Cookie = cookie;
                ReturnUrl = returnUrl;
                WrongCode = mode == 12;
                ResendMailUrl = $"/Account/LoginMessage?m=1&a={accountLink}&r={returnUrl}";
                break;
        }

        return Page();
    }

    public async Task<IActionResult> OnPost(string? emailAddress, string? returnUrl, string? accountLink, string? code, string? cookie)
    {
        if (!string.IsNullOrEmpty(emailAddress))
        {
            var result = await SendMail(emailAddress, null, returnUrl);

            return Redirect($"/Account/LoginMessage?m=11&e={result.EmailAddress}&c={result.Cookie}");
        }

        if (!string.IsNullOrEmpty(code))
        {
            if (string.IsNullOrEmpty(cookie) || string.IsNullOrEmpty(accountLink))
                throw new InvalidOperationException();

            if (!ValidateCookie(cookie, accountLink, code))
            {
                return Redirect($"/Account/LoginMessage?m=12&a={accountLink}&c={cookie}&r={returnUrl}");
            }

            // This can be more efficient, by eliminating one redirect...
            string emailAddressFromAccountLink = await GetEmailAddress(accountLink);
            string callbackUrl = await GetLoginCallbackUrl(emailAddressFromAccountLink, returnUrl);
            return Redirect(callbackUrl);
        }

        throw new InvalidOperationException();
    }
    
    class SendMailResult
    {
        public required string EmailAddress { get; init; } 
        public required string Cookie { get; init; } 
    }

    private async Task<string> GetLoginCallbackUrl(string emailAddress, string? returnUrl)
    {
        string? escapedReturnUrl;
        if (returnUrl == null)
            escapedReturnUrl = null;
        else
            escapedReturnUrl = Uri.EscapeDataString(returnUrl);
        var user = await _userManager.FindByEmailAsync(emailAddress) ?? throw new Exception("User not found");
        string token = await _userManager.GenerateUserTokenAsync(user, "Default", "passwordless-auth");
        
        var url = Url.PageLink("AccountCallback", pageHandler: null, 
            values: new { token = token, email = emailAddress, url = escapedReturnUrl },
            protocol: Request.Scheme);
        return url ?? throw new InvalidOperationException("Could not generate URL for AccountCallback");
    }
    
    private async Task<SendMailResult> SendMail(string emailAddress, string? accountLink, string? returnUrl)
    {

        string code = GenerateCode();
        string url = await GetLoginCallbackUrl(emailAddress, returnUrl);

        await _messenger.SendAuthenticationMailAsync(emailAddress, url, code);

        return new SendMailResult
        {
            EmailAddress = AnonymizeEmail(emailAddress),
            Cookie = GenerateCookie(accountLink ?? emailAddress, code)
        };
    }

    private async Task<string> GetEmailAddress(string accountLink)
    {
        var account = await _mediator.Send(new AccountByLinkQuery
        {
            Link = accountLink
        });

        if (account == null)
        {
            throw new AccountNotFoundException();
        }

        return account.Email;
    }
    
    private async Task<SendMailResult> SendMailToAccountLink(string accountLink, string? returnUrl)
    {
        string email = await GetEmailAddress(accountLink);
        return await SendMail(email, accountLink, returnUrl);
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
    
    private string GenerateCode()
    {
        int number = _random.Next(0, 999999);
        return number.ToString("000 000");
    }

    private string NormalizeCode(string code)
    {
        return String.Concat(code.Where(c => !Char.IsWhiteSpace(c)));
    }

    private string GenerateCookie(string accountLink, string code)
    {
        string normalizedCode = NormalizeCode(code);
        return GenerateCookie(accountLink, normalizedCode, DateTime.UtcNow);
    }

    private string GenerateCookie(string accountLink, string normalizedCode, DateTime timestamp)
    {
        string salt = _options.Salt;
        string time = timestamp.ToString("yyyyMMddHH");
        string combined = string.Concat(salt, accountLink, time, normalizedCode);
        using SHA256 sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
        return Convert.ToBase64String(bytes);
    }

    private bool ValidateCookie(string cookie, string accountLink, string code)
    {
        string normalizedCode = NormalizeCode(code);
        
        DateTime now = DateTime.UtcNow;
        int validityHours = (int)Math.Ceiling((_options.TokenLifespan ?? TimeSpan.Zero).TotalHours);

        for (int hour = 0; hour <= validityHours; hour++)
        {
            string targetCookie = GenerateCookie(accountLink, normalizedCode, now.AddHours(-hour));
            if (cookie.Equals(targetCookie, StringComparison.InvariantCulture))
                return true;
        }

        return false;
    }
}
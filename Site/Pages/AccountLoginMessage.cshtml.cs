using System.Security.Cryptography;
using System.Text;
using Core.Audit;
using Core.Communication;
using Core.Exceptions;
using Core.Queries;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using NetTools;
using Site.Security;

namespace Site.Pages;

public class AccountLoginMessageOptions
{
    public const string Location = "AccountLoginMessage";

    [ConfigurationKeyName("TokenLifespan")]
    public required TimeSpan? TokenLifespanRaw { get; init; }

    [ConfigurationKeyName("CodeLifespanHours")]
    public required int? CodeLifespanHoursRaw { get; init; }

    [ConfigurationKeyName("Salt")]
    public required string SaltRaw { get; init; }

    [ConfigurationKeyName("AdminEmails")]
    public string[]? AdminEmails { get; init; } = null;

    [ConfigurationKeyName("AdminIPs")]
    public string[]? AdminIPsRaw { get; init; } = null;

    public TimeSpan TokenLifespan =>
        TokenLifespanRaw
        ?? throw new Exception("AccountLoginMessageOptions.TokenLifespan not configured");

    public int CodeLifespanHours =>
        CodeLifespanHoursRaw
        ?? throw new Exception("AccountLoginMessageOptions.CodeLifespanHours not configured");

    public string Salt =>
        string.IsNullOrEmpty(SaltRaw)
        ? throw new Exception("AccountLoginMessageOptions.Salt not configured")
        : SaltRaw;

    public IEnumerable<IPAddressRange>? AdminIPs
    {
        get
        {
            if (AdminIPsRaw is null)
                return null;

            return AdminIPsRaw.Select(ipRange => IPAddressRange.Parse(ipRange));
        }
    }
}

public class AccountLoginMessage : PageModel
{
    internal const string AuditActionCodeRequested = "Auth.CodeRequested";
    internal const string AuditActionCodeVerified = "Auth.CodeVerified";
    internal const string UnknownEmailError = "unknown_email";

    private static Random _random = new Random();

    private readonly UserManager<IdentityUser> _userManager;
    private readonly IMediator _mediator;
    private readonly IMessenger _messenger;
    private readonly AccountLoginMessageOptions _options;
    private readonly ILoginSecurityService _loginSecurityService;
    private readonly IKnownLoginEmailService _knownLoginEmailService;
    private readonly IAuditService _auditService;
    private readonly ILogger<AccountLoginMessage> _logger;
    public string? EmailAddress { get; set; }
    public string? AccountLink { get; set; }
    public string? ReturnUrl { get; set; }
    public string? Cookie { get; set; }
    public string? ResendMailUrl { get; set; }
    public bool WrongCode { get; set; }

    public AccountLoginMessage(UserManager<IdentityUser> userManager, IMediator mediator,
        IMessenger messenger,
        IOptions<AccountLoginMessageOptions> options,
        ILoginSecurityService loginSecurityService,
        IKnownLoginEmailService knownLoginEmailService,
        IAuditService auditService,
        ILogger<AccountLoginMessage> logger)
    {
        _userManager = userManager;
        _mediator = mediator;
        _messenger = messenger;
        _options = options.Value;
        _loginSecurityService = loginSecurityService;
        _knownLoginEmailService = knownLoginEmailService;
        _auditService = auditService;
        _logger = logger;
    }
    
    public async Task<IActionResult> OnGet(
        [FromQuery(Name = "m")] int mode = 1, 
        [FromQuery(Name = "a")] string? accountLink = null, 
        [FromQuery(Name = "e")] string? emailAddress = null,
        [FromQuery(Name = "c")] string? cookie = null,
        [FromQuery(Name = "r")] string? returnUrl = null)
    {
        switch (mode)
        {
            case 1:
                if (accountLink != null)
                {
                    using var auditScope = _auditService.BeginAction(AuditActionCodeRequested,
                        new AuditTarget { AccountLink = accountLink });
                    await _auditService.LogAsync(AuditOutcome.Attempted);

                    if (!CanSendCode(accountLink, out var retryAfter))
                    {
                        _logger.LogWarning("Code send throttled for account link from IP {IpAddress}; retry after {RetryAfterSeconds}s", GetClientIpAddress(), Math.Max(1, (int)retryAfter.TotalSeconds));
                        await _auditService.LogAsync(AuditOutcome.Denied, new AuditDetails { Reason = "Rate limited" });
                        return StatusCode(StatusCodes.Status429TooManyRequests);
                    }

                    var result = await SendMailToAccountLink(accountLink, returnUrl);
                    if (result == null)
                        return RedirectToLoginWithError(UnknownEmailError, returnUrl);

                    return Redirect(2, accountLink, result.EmailAddress, result.Cookie, returnUrl);
                }
                return BadRequest();
            case 3:
                {
                    var adminEmail = emailAddress
                        ?? _options.AdminEmails?.FirstOrDefault()
                        ?? throw new InvalidOperationException("No admin email configured");

                    using var auditScope = _auditService.BeginAction(AuditActionCodeRequested,
                        new AuditTarget { Email = adminEmail });
                    await _auditService.LogAsync(AuditOutcome.Attempted);

                    var adminTarget = emailAddress ?? "admin-default";
                    if (!CanSendCode(adminTarget, out var retryAfter))
                    {
                        _logger.LogWarning("Admin code send throttled from IP {IpAddress}; retry after {RetryAfterSeconds}s", GetClientIpAddress(), Math.Max(1, (int)retryAfter.TotalSeconds));
                        await _auditService.LogAsync(AuditOutcome.Denied, new AuditDetails { Reason = "Rate limited" });
                        return StatusCode(StatusCodes.Status429TooManyRequests);
                    }

                    if (!_knownLoginEmailService.IsAdminEmail(adminEmail))
                    {
                        _logger.LogWarning("Admin code requested for non-admin email {Email} from IP {IpAddress}", adminEmail, GetClientIpAddress());
                        await _auditService.LogAsync(AuditOutcome.Denied,
                            new AuditDetails { Reason = "Not an admin email address" });
                        return RedirectToLoginWithError(UnknownEmailError, returnUrl);
                    }

                    var result = await SendMail(adminEmail, null, returnUrl);
                    if (result == null)
                        return RedirectToLoginWithError(UnknownEmailError, returnUrl);

                    return Redirect(2, accountLink, result.EmailAddress, result.Cookie, returnUrl);
                }
            case 2:
                EmailAddress = emailAddress;
                AccountLink = accountLink;
                Cookie = cookie;
                ReturnUrl = returnUrl;
                break;
            case 11:
            case 12:
                EmailAddress = emailAddress;
                AccountLink = accountLink;
                Cookie = cookie;
                ReturnUrl = returnUrl;
                WrongCode = mode == 12;
                ResendMailUrl = GetUrl(1, accountLink, returnUrl);
                break;
        }

        return Page();
    }

    public IActionResult Redirect(int mode, string? accountLink, string? emailAddress, string? cookie, string? returnUrl)
    {
        var url = GetUrl(mode, accountLink, emailAddress, cookie, returnUrl);
        return Redirect(url);
    }

    public static string GetUrl(int mode, string? accountLink, string? returnUrl)
        => GetUrl(mode, accountLink, null, null, returnUrl);

    public static string GetUrl(int mode, string? accountLink, string? emailAddress, string? cookie, string? returnUrl)
    {
        var queryStringBuilder = System.Web.HttpUtility.ParseQueryString(string.Empty);
        queryStringBuilder.Add("m", mode.ToString());
        if (accountLink != null)
            queryStringBuilder.Add("a", accountLink);
        if (emailAddress != null)
            queryStringBuilder.Add("e", emailAddress);
        if (cookie != null)
            queryStringBuilder.Add("c", cookie);
        if (returnUrl != null)
            queryStringBuilder.Add("r", returnUrl);

        return $"/Account/LoginMessage?{queryStringBuilder}";
    }

    public async Task<IActionResult> OnPost(
        int mode,
        string? accountLink,
        string? emailAddress,
        string? cookie,
        string? returnUrl,
        string? code)
    {
        if (mode == 21 && !string.IsNullOrEmpty(emailAddress))
        {
            using var auditScope = _auditService.BeginAction(AuditActionCodeRequested,
                new AuditTarget { Email = emailAddress });
            await _auditService.LogAsync(AuditOutcome.Attempted);

            if (!CanSendCode(emailAddress, out var retryAfter))
            {
                _logger.LogWarning("Code resend throttled for email from IP {IpAddress}; retry after {RetryAfterSeconds}s", GetClientIpAddress(), Math.Max(1, (int)retryAfter.TotalSeconds));
                await _auditService.LogAsync(AuditOutcome.Denied, new AuditDetails { Reason = "Rate limited" });
                return StatusCode(StatusCodes.Status429TooManyRequests);
            }

            var result = await SendMail(emailAddress, null, returnUrl);
            if (result == null)
                return RedirectToLoginWithError(UnknownEmailError, returnUrl);

            return Redirect(11, accountLink, result.EmailAddress, result.Cookie, returnUrl);
        }

        if (mode == 22 && !string.IsNullOrEmpty(code))
        {
            if (string.IsNullOrEmpty(cookie))
                throw new InvalidOperationException();

            var target = accountLink ?? emailAddress ?? string.Empty;
            if (string.IsNullOrEmpty(target))
                throw new InvalidOperationException("No account link or email address provided.");

            using var auditScope = _auditService.BeginAction(AuditActionCodeVerified,
                new AuditTarget { AccountLink = accountLink, Email = emailAddress });
            await _auditService.LogAsync(AuditOutcome.Attempted);

            if (!CanVerifyCode(target, out var retryAfter))
            {
                _logger.LogWarning("Code verification throttled from IP {IpAddress}; retry after {RetryAfterSeconds}s", GetClientIpAddress(), Math.Max(1, (int)retryAfter.TotalSeconds));
                await _auditService.LogAsync(AuditOutcome.Denied, new AuditDetails { Reason = "Rate limited" });
                return Redirect(12, accountLink, emailAddress, cookie, returnUrl);
            }

            if (!ValidateCookie(cookie, target, code))
            {
                _loginSecurityService.RecordVerifyResult(GetClientIpAddress(), target, success: false);
                _logger.LogWarning("Code verification failed for login target from IP {IpAddress}", GetClientIpAddress());
                await _auditService.LogAsync(AuditOutcome.Failed, new AuditDetails { Reason = "Invalid code" });
                return Redirect(12, accountLink, emailAddress, cookie, returnUrl);
            }

            _loginSecurityService.RecordVerifyResult(GetClientIpAddress(), target, success: true);
            _logger.LogInformation("Code verification succeeded for login target from IP {IpAddress}", GetClientIpAddress());
            await _auditService.LogAsync(AuditOutcome.Succeeded);

            // This can be more efficient, by eliminating one redirect...
            string emailAddressLogin;
            if (accountLink != null)
                emailAddressLogin = await GetEmailAddress(accountLink);
            else if (!string.IsNullOrEmpty(emailAddress))
                emailAddressLogin = emailAddress;
            else
                throw new InvalidOperationException("No account link or email address provided.");

            string callbackUrl = await GetLoginCallbackUrl(emailAddressLogin, returnUrl);
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
    
    public static string GetLogoutUrl(PageModel page, string? returnUrl)
    {
        return GetLogoutUrl(page.Url, page.Request.Scheme, returnUrl);
    }

    public static string GetLogoutUrl(IUrlHelper urlHelper, string scheme, string? returnUrl)
    {
        string? escapedReturnUrl = returnUrl != null ? Uri.EscapeDataString(returnUrl) : null;

        var url = urlHelper.PageLink("/AccountCallback", pageHandler: null,
            values: new { token = "", email = "", url = escapedReturnUrl },
            protocol: scheme);
        return url ?? throw new InvalidOperationException("Could not generate URL for AccountCallback");
    }
    
    /// <summary>
    /// Sends a login code to the email address.  Returns null when the address is not known,
    /// in which case no mail is sent and the rejection is audited.
    /// </summary>
    private async Task<SendMailResult?> SendMail(string emailAddress, string? accountLink, string? returnUrl)
    {
        if (!await _knownLoginEmailService.IsKnownLoginEmail(emailAddress))
        {
            _logger.LogWarning("Authentication code requested for unknown email {Email} from IP {IpAddress}", emailAddress, GetClientIpAddress());
            await _auditService.LogAsync(AuditOutcome.Denied,
                new AuditDetails { Reason = "Unknown email address" },
                target: new AuditTarget { Email = emailAddress });
            return null;
        }

        string code = GenerateCode();
        string url = await GetLoginCallbackUrl(emailAddress, returnUrl);

        _logger.LogInformation("Authentication code requested for email {Email} from IP {IpAddress}", emailAddress, GetClientIpAddress());

        if (!Core.Entities.Account.IsDemoEmail(emailAddress))
        {
            await _messenger.SendAuthenticationMailAsync(emailAddress, url, code);
        }

        await _auditService.LogAsync(AuditOutcome.Succeeded, target: new AuditTarget { Email = emailAddress });

        if (!string.IsNullOrEmpty(accountLink))
        {
            emailAddress = AnonymizeEmail(emailAddress);
        }

        return new SendMailResult
        {
            EmailAddress = emailAddress,
            Cookie = GenerateCookie(accountLink ?? emailAddress, code)
        };
    }

    private IActionResult RedirectToLoginWithError(string error, string? returnUrl)
    {
        return RedirectToPage("/Login", new { error, r = returnUrl });
    }

    private bool CanSendCode(string target, out TimeSpan retryAfter)
    {
        return _loginSecurityService.CanSendCode(GetClientIpAddress(), target, out retryAfter);
    }

    private bool CanVerifyCode(string target, out TimeSpan retryAfter)
    {
        return _loginSecurityService.CanVerifyCode(GetClientIpAddress(), target, out retryAfter);
    }

    private string GetClientIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
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
    
    private async Task<SendMailResult?> SendMailToAccountLink(string accountLink, string? returnUrl)
    {
        string email;
        try
        {
            email = await GetEmailAddress(accountLink);
        }
        catch (AccountNotFoundException)
        {
            await _auditService.LogAsync(AuditOutcome.Failed, new AuditDetails { Reason = "Account not found" });
            throw;
        }

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
        int validityHours = _options.CodeLifespanHours;

        for (int hour = 0; hour <= validityHours; hour++)
        {
            string targetCookie = GenerateCookie(accountLink, normalizedCode, now.AddHours(-hour));
            if (cookie.Equals(targetCookie, StringComparison.InvariantCulture))
                return true;
        }

        return false;
    }
}
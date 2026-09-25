using System.Security.Claims;
using System.Text.Json;
using Core.Commands;
using Core.Queries;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CoreEntities = Core.Entities;

namespace Site.Pages;

public class AccountPicker : PageModel
{
    private readonly IMediator _mediator;
    private readonly IDataProtectionProvider _dataProtectionProvider;
    private readonly ILogger<AccountPicker> _logger;

    public IReadOnlyList<CoreEntities.Account> Accounts { get; private set; } = [];

    /// <summary>
    /// "oidc"   = first-time external login needing account selection.
    /// "switch" = already authenticated, switching active account.
    /// </summary>
    public string Mode { get; private set; } = "switch";

    public AccountPicker(IMediator mediator, IDataProtectionProvider dataProtectionProvider, ILogger<AccountPicker> logger)
    {
        _mediator = mediator;
        _dataProtectionProvider = dataProtectionProvider;
        _logger = logger;
    }

    // GET /account-picker
    public async Task<IActionResult> OnGetAsync()
    {
        var pickerToken = ReadPickerToken();
        if (pickerToken != null)
        {
            Mode = "oidc";
            Accounts = await _mediator.Send(new AccountsByEmailQuery { Email = pickerToken.Email });
            return Page();
        }

        // No picker cookie — must be an authenticated switch request
        if (User.Identity?.IsAuthenticated != true)
            return RedirectToPage("/Login", new { error = "not_authenticated" });

        Mode = "switch";
        Accounts = await GetAccessibleAccounts();
        return Page();
    }

    // POST handler: user selected an account during external first-login
    public async Task<IActionResult> OnPostOidcAsync(
        int accountId,
        [FromServices] IConfiguration configuration)
    {
        var pickerToken = ReadPickerToken();
        if (pickerToken == null)
            return RedirectToPage("/Login", new { error = "picker_expired" });

        var accounts = await _mediator.Send(new AccountsByEmailQuery { Email = pickerToken.Email });
        CoreEntities.Account? account = accounts.FirstOrDefault(a => a.Id == accountId);
        if (account == null)
        {
            _logger.LogWarning(
                "AccountPicker: account {AccountId} not in allowed list for email {Email}",
                accountId, pickerToken.Email);
            return Forbid();
        }

        // Auto-link the subject to the selected account (unless already linked)
        var existingLink = await _mediator.Send(new AccountUserByProviderQuery
        {
            Provider = OidcCallback.ProviderName,
            ProviderSubjectId = pickerToken.ProviderSub
        });

        if (existingLink == null)
        {
            await _mediator.Send(new AddAccountUserCommand
            {
                AccountId = account.Id,
                LoginType = CoreEntities.AccountUserLoginType.Oidc,
                Email = pickerToken.Email,
                Provider = OidcCallback.ProviderName,
                ProviderSubjectId = pickerToken.ProviderSub
            });
        }

        DeletePickerCookie();

        return await SignInAccount(account, pickerToken.ProviderSub, pickerToken.ReturnUrl, configuration);
    }

    // POST handler: already-authenticated user switching to a different account
    public async Task<IActionResult> OnPostSwitchAsync(
        int accountId,
        [FromServices] IConfiguration configuration)
    {
        if (User.Identity?.IsAuthenticated != true)
            return Forbid();

        var accessibleAccounts = await GetAccessibleAccounts();
        CoreEntities.Account? account = accessibleAccounts.FirstOrDefault(a => a.Id == accountId);
        if (account == null)
        {
            _logger.LogWarning(
                "AccountPicker switch: account {AccountId} not accessible for current session",
                accountId);
            return Forbid();
        }

        var providerSub = User.FindFirstValue("provider_sub");
        return await SignInAccount(account, providerSub, null, configuration);
    }

    private async Task<IReadOnlyList<CoreEntities.Account>> GetAccessibleAccounts()
    {
        var email = User.FindFirstValue("email");
        var providerSub = User.FindFirstValue("provider_sub");
        // Use the session's own provider so legacy "google" sessions keep resolving.
        var provider = User.FindFirstValue("provider") ?? OidcCallback.ProviderName;

        var accountsById = new Dictionary<int, CoreEntities.Account>();

        if (!string.IsNullOrEmpty(email))
        {
            var byEmail = await _mediator.Send(new AccountsByEmailQuery { Email = email });
            foreach (var a in byEmail)
                accountsById[a.Id] = a;
        }

        if (!string.IsNullOrEmpty(providerSub))
        {
            var providerUser = await _mediator.Send(new AccountUserByProviderQuery
            {
                Provider = provider,
                ProviderSubjectId = providerSub
            });
            if (providerUser != null)
            {
                var providerAccount = await _mediator.Send(new AccountByIdQuery { Id = providerUser.AccountId });
                if (providerAccount != null)
                    accountsById[providerAccount.Id] = providerAccount;
            }
        }

        return accountsById.Values.OrderBy(a => a.Id).ToList();
    }

    private AccountPickerToken? ReadPickerToken()
    {
        if (!Request.Cookies.TryGetValue(OidcCallback.PickerCookieName, out var cookieValue))
            return null;

        try
        {
            var protector = _dataProtectionProvider.CreateProtector(OidcCallback.PickerProtectionPurpose);
            var json = protector.Unprotect(cookieValue);
            return JsonSerializer.Deserialize<AccountPickerToken>(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AccountPicker: failed to read/unprotect picker cookie");
            return null;
        }
    }

    private void DeletePickerCookie()
    {
        Response.Cookies.Delete(OidcCallback.PickerCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        });
    }

    private async Task<IActionResult> SignInAccount(
        CoreEntities.Account account,
        string? providerSub,
        string? returnUrl,
        IConfiguration configuration)
    {
        var claims = new List<Claim>
        {
            new("sub", account.Uid.ToString()),
            new("email", account.Email),
            new("auth_method", OidcCallback.ProviderName),
            new("provider", OidcCallback.ProviderName)
        };
        if (!string.IsNullOrEmpty(providerSub))
            claims.Add(new Claim("provider_sub", providerSub));

        var configOptions = configuration
            .GetSection(AccountLoginMessageOptions.Location)
            .Get<AccountLoginMessageOptions>()
            ?? throw new InvalidOperationException("AccountLoginMessageOptions not configured");

        await HttpContext.SignInAsync(
            IdentityConstants.ApplicationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, IdentityConstants.ApplicationScheme)),
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.Add(configOptions.TokenLifespan)
            });

        _logger.LogInformation(
            "AccountPicker: signed in to account {AccountId} from IP {IpAddress}",
            account.Id, HttpContext.Connection.RemoteIpAddress);

        // returnUrl originated from a query parameter: never redirect off-site with it.
        return Redirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/auto");
    }
}

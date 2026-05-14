using System.Security.Claims;
using Core.Entities;
using Core.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Site.Utilities;

public interface IUserInfo
{
    bool IsAuthenticated();
    string? GetLoginEmail();
    string? GetCurrentAccountSub();
    Task<bool> CanUpdateAccount(Account account);
    Task<bool> CanUpdateAccountSensor(AccountSensor accountSensor);
    Task<bool> IsAdmin();
    Task<IReadOnlyList<Account>> GetAccessibleAccounts();
}

public class UserInfo : IUserInfo
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuthorizationService _authorizationService;
    private readonly IMediator _mediator;
    private readonly Pages.AccountLoginMessageOptions _options;

    public UserInfo(IHttpContextAccessor httpContextAccessor,
        IAuthorizationService authorizationService,
        IMediator mediator,
        IOptions<Pages.AccountLoginMessageOptions> options)
    {
        _httpContextAccessor = httpContextAccessor;
        _authorizationService = authorizationService;
        _mediator = mediator;
        _options = options.Value;
    }

    public bool IsAuthenticated()
    {
        return _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
    }

    public string? GetLoginEmail()
    {
        return _httpContextAccessor.HttpContext?.User.FindFirstValue("email");
    }

    public string? GetCurrentAccountSub()
    {
        return _httpContextAccessor.HttpContext?.User.FindFirstValue("sub");
    }

    public async Task<bool> CanUpdateAccount(Account account)
    {
        if (await IsAdmin())
            return true;

        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return false;

        var email = user.FindFirstValue("email");
        var provider = user.FindFirstValue("provider");
        var providerSub = user.FindFirstValue("provider_sub");

        var accountUsers = await _mediator.Send(new AccountUsersByAccountQuery { AccountId = account.Id });
        return accountUsers.Any(u =>
            (u.LoginType == AccountUserLoginType.Mail
                && email != null
                && string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase))
            ||
            (u.LoginType == AccountUserLoginType.Google
                && provider == "google"
                && providerSub != null
                && u.ProviderSubjectId == providerSub));
    }

    public async Task<bool> CanUpdateAccountSensor(AccountSensor accountSensor)
    {
        return await CanUpdateAccount(accountSensor.Account);
    }

    public async Task<bool> IsAdmin()
    {
        if (!(_httpContextAccessor.HttpContext is { } httpContext))
            return false;

        if (!(httpContext.User is { } user))
            return false;

        var result = await _authorizationService.AuthorizeAsync(user, "Admin");
        return result.Succeeded;
    }

    public async Task<IReadOnlyList<Account>> GetAccessibleAccounts()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return [];

        var email = user.FindFirstValue("email");
        var providerSub = user.FindFirstValue("provider_sub");

        var accountsById = new Dictionary<int, Account>();

        if (!string.IsNullOrEmpty(email))
        {
            var byEmail = await _mediator.Send(new AccountsByEmailQuery { Email = email });
            foreach (var a in byEmail)
                accountsById[a.Id] = a;
        }

        if (!string.IsNullOrEmpty(providerSub))
        {
            var googleUser = await _mediator.Send(new AccountUserByProviderQuery
            {
                Provider = "google",
                ProviderSubjectId = providerSub
            });
            if (googleUser != null)
            {
                var googleAccount = await _mediator.Send(new AccountByIdQuery { Id = googleUser.AccountId });
                if (googleAccount != null)
                    accountsById[googleAccount.Id] = googleAccount;
            }
        }

        return accountsById.Values.OrderBy(a => a.Id).ToList();
    }
}
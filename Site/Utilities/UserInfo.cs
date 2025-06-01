using System.Runtime.CompilerServices;
using System.Security.Claims;
using Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Site.Utilities;

public interface IUserInfo
{
    bool IsAuthenticated();
    string? GetLoginEmail();
    bool CanUpdateAccount(Account account);
    bool CanUpdateAccountSensor(AccountSensor accountSensor);
    Task<bool> IsAdmin();
}

public class UserInfo : IUserInfo
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuthorizationService _authorizationService;
    private readonly Pages.AccountLoginMessageOptions _options;

    public UserInfo(IHttpContextAccessor httpContextAccessor,
        IAuthorizationService authorizationService,
        IOptions<Pages.AccountLoginMessageOptions> options)
    {
        _httpContextAccessor = httpContextAccessor;
        _authorizationService = authorizationService;
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

    public bool CanUpdateAccount(Account account)
    {
        var loginEmail = GetLoginEmail();

        if (string.IsNullOrEmpty(loginEmail))
            return false;

        return string.Equals(loginEmail, account.Email, StringComparison.OrdinalIgnoreCase);
    }

    public bool CanUpdateAccountSensor(AccountSensor accountSensor)
    {
        return CanUpdateAccount(accountSensor.Account);
    }

    public async Task<bool> IsAdmin()
    {
        if (!(_httpContextAccessor.HttpContext?.User is { } user))
            return false;

        var result = await _authorizationService.AuthorizeAsync(user, "Admin");
        return result.Succeeded;
    }
}
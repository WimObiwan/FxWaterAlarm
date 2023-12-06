using System.Security.Claims;
using Core.Entities;

namespace Site.Utilities;

public interface IUserInfo
{
    bool IsAuthenticated();
    string? GetLoginEmail();
    bool CanUpdateAccount(Account account);
    bool CanUpdateAccountSensor(AccountSensor accountSensor);
}

public class UserInfo : IUserInfo
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserInfo(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
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
}
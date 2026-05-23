using Core.Commands;
using Core.Entities;
using Core.Queries;
using Core.Util;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using Site.Authentication;
using Site.Utilities;

namespace Site.Pages;

[Authorize]
public class AccountUsers : PageModel
{
    private readonly IMediator _mediator;
    private readonly IUserInfo _userInfo;

    public Core.Entities.Account? AccountEntity { get; private set; }
    public IReadOnlyList<AccountUser> Users { get; private set; } = [];
    public bool GoogleEnabled { get; private set; }
    public bool IsAdmin { get; private set; }
    public int? EditEmailUserId { get; private set; }
    public string? Message { get; private set; }

    public AccountUsers(IMediator mediator, IUserInfo userInfo)
    {
        _mediator = mediator;
        _userInfo = userInfo;
    }

    public async Task<IActionResult> OnGet(
        [FromRoute] string accountLink,
        [FromQuery] string? message,
        [FromQuery] int? editEmailUserId,
        [FromServices] IOptionsSnapshot<GoogleAuthOptions>? googleOptions = null)
    {
        Message = message;
        GoogleEnabled = googleOptions?.Value.IsConfigured ?? false;

        AccountEntity = await _mediator.Send(new AccountByLinkQuery { Link = accountLink });
        if (AccountEntity == null)
            return NotFound();

        if (!await _userInfo.CanUpdateAccount(AccountEntity))
            return Forbid();

        IsAdmin = await _userInfo.IsAdmin();

        Users = await _mediator.Send(new AccountUsersByAccountQuery { AccountId = AccountEntity.Id });

        if (editEmailUserId.HasValue)
        {
            var requestedUser = Users.SingleOrDefault(u => u.Id == editEmailUserId.Value);
            if (requestedUser != null && CanEditDefaultEmail(requestedUser))
                EditEmailUserId = requestedUser.Id;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAddMailUserAsync(
        [FromRoute] string accountLink,
        [FromForm] string email,
        [FromServices] IOptionsSnapshot<GoogleAuthOptions>? googleOptions = null)
    {
        GoogleEnabled = googleOptions?.Value.IsConfigured ?? false;

        var account = await _mediator.Send(new AccountByLinkQuery { Link = accountLink });
        if (account == null)
            return NotFound();

        if (!await _userInfo.CanUpdateAccount(account))
            return Forbid();

        email = email.Trim();
        if (string.IsNullOrWhiteSpace(email))
            return Redirect($"/a/{accountLink}/users");

        var users = await _mediator.Send(new AccountUsersByAccountQuery { AccountId = account.Id });
        if (users.Any(u => u.LoginType == AccountUserLoginType.Mail
            && u.Email != null
            && string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)))
        {
            return Redirect($"/a/{accountLink}/users?message=user_already_exists");
        }

        await _mediator.Send(new AddAccountUserCommand
        {
            AccountId = account.Id,
            LoginType = AccountUserLoginType.Mail,
            Email = email.Trim()
        });

        return Redirect($"/a/{accountLink}/users?message=user_added");
    }

    public IActionResult OnGetLinkGoogle(
        [FromRoute] string accountLink,
        [FromServices] IOptionsSnapshot<GoogleAuthOptions>? googleOptions = null)
    {
        if (!(googleOptions?.Value.IsConfigured ?? false))
            return Redirect($"/a/{accountLink}/users?message=google_not_configured");

        var callbackUrl = Url.Page("/GoogleCallback", values: new { mode = "link", a = accountLink });
        var properties = new AuthenticationProperties { RedirectUri = callbackUrl };
        return Challenge(properties, "Google");
    }

    public async Task<IActionResult> OnPostRemoveUserAsync(
        [FromRoute] string accountLink,
        [FromForm] int userId)
    {
        var account = await _mediator.Send(new AccountByLinkQuery { Link = accountLink });
        if (account == null)
            return NotFound();

        if (!await _userInfo.CanUpdateAccount(account))
            return Forbid();

        var users = await _mediator.Send(new AccountUsersByAccountQuery { AccountId = account.Id });
        var user = users.SingleOrDefault(u => u.Id == userId);
        if (user == null)
            return NotFound();

        if (IsCurrentUser(user))
            return Redirect($"/a/{accountLink}/users?message=user_cannot_remove_current");

        await _mediator.Send(new RemoveAccountUserCommand { AccountUserId = userId });
        return Redirect($"/a/{accountLink}/users?message=user_removed");
    }

    public async Task<IActionResult> OnPostChangeEmailAsync(
        [FromRoute] string accountLink,
        [FromForm] int userId,
        [FromForm] string email)
    {
        var account = await _mediator.Send(new AccountByLinkQuery { Link = accountLink });
        if (account == null)
            return NotFound();

        if (!await _userInfo.IsAdmin())
            return Forbid();

        email = email.Trim();
        if (string.IsNullOrWhiteSpace(email))
            return Redirect($"/a/{accountLink}/users?message=email_invalid");

        var users = await _mediator.Send(new AccountUsersByAccountQuery { AccountId = account.Id });
        var user = users.SingleOrDefault(u => u.Id == userId);
        if (user == null || !IsDefaultMailUser(account, user))
            return Redirect($"/a/{accountLink}/users");

        if (users.Any(u => u.Id != user.Id
            && u.LoginType == AccountUserLoginType.Mail
            && u.Email != null
            && string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)))
        {
            return Redirect($"/a/{accountLink}/users?message=user_already_exists");
        }

        await _mediator.Send(new UpdateAccountCommand
        {
            Uid = account.Uid,
            Email = Optional.From(email)
        });

        await _mediator.Send(new RemoveAccountUserCommand { AccountUserId = user.Id });
        await _mediator.Send(new AddAccountUserCommand
        {
            AccountId = account.Id,
            LoginType = AccountUserLoginType.Mail,
            Email = email
        });

        return Redirect($"/a/{accountLink}/users?message=email_changed");
    }

    public bool CanRemoveUser(AccountUser user)
    {
        return !IsCurrentUser(user);
    }

    public bool CanEditDefaultEmail(AccountUser user)
    {
        return IsAdmin && AccountEntity != null && IsDefaultMailUser(AccountEntity, user);
    }

    private static bool IsDefaultMailUser(Core.Entities.Account account, AccountUser user)
    {
        return user.LoginType == AccountUserLoginType.Mail
            && user.Email != null
            && string.Equals(user.Email, account.Email, StringComparison.OrdinalIgnoreCase);
    }

    private bool IsCurrentUser(AccountUser user)
    {
        var principal = HttpContext.User;
        if (principal.Identity?.IsAuthenticated != true)
            return false;

        var email = principal.FindFirstValue("email");
        var provider = principal.FindFirstValue("provider");
        var providerSub = principal.FindFirstValue("provider_sub");

        return (user.LoginType == AccountUserLoginType.Mail
                && email != null
                && user.Email != null
                && string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
            || (user.LoginType == AccountUserLoginType.Google
                && provider == "google"
                && providerSub != null
                && user.ProviderSubjectId == providerSub);
    }
}

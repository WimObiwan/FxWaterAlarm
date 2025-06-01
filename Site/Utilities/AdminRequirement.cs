using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Site.Pages;

public class AdminRequirement : IAuthorizationRequirement
{
}

public class AdminRequirementHandler : AuthorizationHandler<AdminRequirement>
{
    private readonly IOptions<AccountLoginMessageOptions> _options;

    public AdminRequirementHandler(IOptions<AccountLoginMessageOptions> options)
    {
        _options = options;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AdminRequirement requirement)
    {
        var loginEmail = context.User.FindFirst("email")?.Value;
        var adminEmails = _options.Value.AdminEmails;

        if (
            !string.IsNullOrEmpty(loginEmail)
            && adminEmails != null
            && adminEmails.Contains(loginEmail, StringComparer.InvariantCultureIgnoreCase)
        )
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }

        return Task.CompletedTask;
    }
}
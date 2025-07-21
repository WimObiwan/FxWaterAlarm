using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Site.Pages;

public class AdminRequirement : IAuthorizationRequirement
{
}

public class AdminRequirementHandler : AuthorizationHandler<AdminRequirement>
{
    private readonly IOptions<AccountLoginMessageOptions> _options;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AdminRequirementHandler> _logger;

    public AdminRequirementHandler(
        IOptions<AccountLoginMessageOptions> options,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AdminRequirementHandler> logger
    )
    {
        _options = options;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AdminRequirement requirement)
    {
        var loginEmail = context.User.FindFirst("email")?.Value;
        var adminEmails = _options.Value.AdminEmails;
        var adminIPs = _options.Value.AdminIPs;
        var httpContext = _httpContextAccessor.HttpContext;

        var remoteIpAddress = httpContext?.Connection.RemoteIpAddress;

        
        _logger.LogWarning("Using IpAddress: {IPAddress}", httpContext?.Connection.RemoteIpAddress);

        if (
            !string.IsNullOrEmpty(loginEmail)
            && adminEmails != null
            && adminEmails.Contains(loginEmail, StringComparer.InvariantCultureIgnoreCase)
            && httpContext != null
            && (adminIPs == null || adminIPs.Any(ipRange => ipRange.Contains(remoteIpAddress)))
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
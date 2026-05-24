using System.Security.Claims;
using Core.Audit;
using Site.Utilities;

namespace Site.Middlewares;

public static class AuditContextMiddlewareExtensions
{
    public static IApplicationBuilder UseAuditContext(this IApplicationBuilder app)
    {
        app.UseMiddleware<AuditContextMiddleware>();
        return app;
    }
}

public class AuditContextMiddleware : IMiddleware
{
    private readonly IAuditScopeAccessor _auditScopeAccessor;
    private readonly IUserInfo _userInfo;

    public AuditContextMiddleware(IAuditScopeAccessor auditScopeAccessor, IUserInfo userInfo)
    {
        _auditScopeAccessor = auditScopeAccessor;
        _userInfo = userInfo;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var identity = ResolveIdentity(context.User);
        var authType = ResolveAuthType(context.User);
        var isAdmin = _userInfo.IsAuthenticated() && await _userInfo.IsAdmin();

        var scope = new AuditScopeContext
        {
            CorrelationId = context.TraceIdentifier,
            Actor = new AuditActor
            {
                Identity = identity,
                AuthType = authType,
                IsAdmin = isAdmin
            },
            Client = new AuditClient
            {
                IpAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                UserAgent = context.Request.Headers["User-Agent"].ToString(),
                RequestPath = context.Request.Path.HasValue ? context.Request.Path.Value : null
            }
        };

        using (_auditScopeAccessor.Push(scope))
        {
            await next(context);
        }
    }

    private static string ResolveIdentity(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true)
            return "anonymous";

        return user.FindFirstValue("email")
            ?? user.FindFirstValue("sub")
            ?? user.FindFirstValue("provider_sub")
            ?? user.Identity?.Name
            ?? "authenticated";
    }

    private static string ResolveAuthType(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true)
            return "Anonymous";

        if (user.HasClaim(c => c.Type == "ApiKey"))
            return "ApiKey";

        return "Cookie";
    }
}

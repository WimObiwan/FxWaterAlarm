using System.Security.Claims;
using Core.Entities;
using Core.Queries;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Site.Authentication;
using Site.Pages;
using Account = Core.Entities.Account;
using SiteTests.Helpers;

namespace SiteTests.Pages;

/// <summary>
/// Tests for the OidcCallback page model (Keycloak login).
/// </summary>
public class OidcCallbackTest
{
    private const string LoginEmail = "second.user@gmail.com";
    private const string AccountEmail = "owner@example.com";
    private const string Subject = "keycloak-sub-1";

    private readonly ConfigurableFakeMediator _mediator = new();
    private readonly FakeAuditService _auditService = new();
    private readonly FakeAuthenticationService _authenticationService = new();

    private readonly Account _account = new()
    {
        Id = 1, Uid = Guid.NewGuid(), Email = AccountEmail, CreationTimestamp = DateTime.UtcNow
    };

    private OidcCallback CreateModel()
    {
        var model = new OidcCallback(_mediator, _auditService, NullLogger<OidcCallback>.Instance,
            new EphemeralDataProtectionProvider());

        var services = new ServiceCollection();
        services.AddSingleton<IAuthenticationService>(_authenticationService);
        var httpContext = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };

        var actionContext = new ActionContext(httpContext, new RouteData(), new PageActionDescriptor());
        model.PageContext = new PageContext(actionContext);
        model.Url = new UrlHelper(actionContext);
        return model;
    }

    private static IConfiguration CreateConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AccountLoginMessage:TokenLifespan"] = TimeSpan.FromHours(24).ToString(),
                ["AccountLoginMessage:CodeLifespanHours"] = "2",
                ["AccountLoginMessage:Salt"] = "test-salt"
            })
            .Build();

    private void SetExternalLogin(string email, string? idToken = "the-id-token")
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, Subject),
            new Claim(ClaimTypes.Email, email),
            new Claim("email_verified", "true")
        ], "oidc");
        var properties = new AuthenticationProperties();
        OidcSession.StoreIdToken(properties, idToken);
        _authenticationService.ExternalResult = AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), properties, "ExternalCookie"));
    }

    [Fact]
    public async Task OnGet_LinkedUser_EmailClaimIsLoginIdentityNotAccountEmail()
    {
        SetExternalLogin(LoginEmail);
        _mediator.SetResponse<AccountUserByProviderQuery, AccountUser?>(new AccountUser
        {
            AccountId = _account.Id, LoginType = AccountUserLoginType.Oidc, Email = LoginEmail,
            Provider = "oidc", ProviderSubjectId = Subject, CreationTimestamp = DateTime.UtcNow
        });
        _mediator.SetResponse<AccountByIdQuery, Account?>(_account);

        var result = await CreateModel().OnGet(null, null, null, CreateConfiguration());

        Assert.IsType<RedirectResult>(result);
        var principal = Assert.Single(_authenticationService.SignedIn).Principal;
        Assert.Equal(LoginEmail, principal.FindFirstValue("email"));
        Assert.Equal(_account.Uid.ToString(), principal.FindFirstValue("sub"));
    }

    [Fact]
    public async Task OnGet_MatchedByEmail_EmailClaimIsLoginIdentity()
    {
        SetExternalLogin(LoginEmail);
        _mediator.SetResponse<AccountsByEmailQuery, IReadOnlyList<Account>>([_account]);

        await CreateModel().OnGet(null, null, null, CreateConfiguration());

        var principal = Assert.Single(_authenticationService.SignedIn).Principal;
        Assert.Equal(LoginEmail, principal.FindFirstValue("email"));
    }

    [Fact]
    public async Task OnGet_SignIn_KeepsIdTokenForLogout()
    {
        SetExternalLogin(LoginEmail);
        _mediator.SetResponse<AccountsByEmailQuery, IReadOnlyList<Account>>([_account]);

        await CreateModel().OnGet(null, null, null, CreateConfiguration());

        var properties = Assert.Single(_authenticationService.SignedIn).Properties;
        Assert.Equal("the-id-token", OidcSession.GetIdToken(properties));
    }

    [Fact]
    public async Task OnGet_UnknownEmail_SignsOutOfKeycloakAndShowsError()
    {
        SetExternalLogin("unknown@gmail.com");
        _mediator.SetResponse<AccountsByEmailQuery, IReadOnlyList<Account>>([]);

        var result = await CreateModel().OnGet(null, null, null, CreateConfiguration());

        var signOut = Assert.IsType<SignOutResult>(result);
        Assert.Equal([OidcSession.Scheme], signOut.AuthenticationSchemes);
        Assert.Contains("error=no_account", signOut.Properties!.RedirectUri);
        Assert.Equal("the-id-token", signOut.Properties.Items[OidcSession.IdTokenHintItem]);
        Assert.Empty(_authenticationService.SignedIn);
    }

    private class FakeAuthenticationService : IAuthenticationService
    {
        public AuthenticateResult ExternalResult { get; set; } = AuthenticateResult.NoResult();
        public List<(ClaimsPrincipal Principal, AuthenticationProperties? Properties)> SignedIn { get; } = new();

        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
            => Task.FromResult(scheme == "ExternalCookie" ? ExternalResult : AuthenticateResult.NoResult());

        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal,
            AuthenticationProperties? properties)
        {
            if (scheme == IdentityConstants.ApplicationScheme)
                SignedIn.Add((principal, properties));
            return Task.CompletedTask;
        }

        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;
    }
}

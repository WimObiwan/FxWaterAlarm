using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace SiteTests.Helpers;

/// <summary>
/// Minimal fake UserManager for testing page models that depend on UserManager&lt;IdentityUser&gt;.
/// Override FindByEmailAsync and GenerateUserTokenAsync / VerifyUserTokenAsync.
/// </summary>
public class FakeUserManager : UserManager<IdentityUser>
{
    private readonly Dictionary<string, IdentityUser> _users = new(StringComparer.OrdinalIgnoreCase);
    public string TokenToReturn { get; set; } = "fake-token";
    public bool VerifyTokenResult { get; set; } = true;

    public FakeUserManager()
        : base(
            new FakeUserStore(),
            Microsoft.Extensions.Options.Options.Create(new IdentityOptions()),
            new PasswordHasher<IdentityUser>(),
            Array.Empty<IUserValidator<IdentityUser>>(),
            Array.Empty<IPasswordValidator<IdentityUser>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!, // IServiceProvider
            NullLogger<UserManager<IdentityUser>>.Instance)
    {
    }

    public void AddUser(string email)
    {
        var user = new IdentityUser { Id = Guid.NewGuid().ToString(), UserName = email, Email = email };
        _users[email] = user;
    }

    public override Task<IdentityUser?> FindByEmailAsync(string email)
    {
        _users.TryGetValue(email, out var user);
        return Task.FromResult(user);
    }

    public override Task<string> GenerateUserTokenAsync(IdentityUser user, string tokenProvider, string purpose)
    {
        return Task.FromResult(TokenToReturn);
    }

    public override Task<bool> VerifyUserTokenAsync(IdentityUser user, string tokenProvider, string purpose, string token)
    {
        return Task.FromResult(VerifyTokenResult);
    }

    public override Task<IdentityResult> UpdateSecurityStampAsync(IdentityUser user)
    {
        return Task.FromResult(IdentityResult.Success);
    }

    private class FakeUserStore : IUserStore<IdentityUser>
    {
        public void Dispose() { }

        public Task<IdentityUser?> FindByIdAsync(string userId, CancellationToken cancellationToken = default)
            => Task.FromResult<IdentityUser?>(null);

        public Task<IdentityUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken = default)
            => Task.FromResult<IdentityUser?>(null);

        public Task<string> GetUserIdAsync(IdentityUser user, CancellationToken cancellationToken = default)
            => Task.FromResult(user.Id);

        public Task<string?> GetUserNameAsync(IdentityUser user, CancellationToken cancellationToken = default)
            => Task.FromResult(user.UserName);

        public Task<string?> GetNormalizedUserNameAsync(IdentityUser user, CancellationToken cancellationToken = default)
            => Task.FromResult(user.NormalizedUserName);

        public Task SetUserNameAsync(IdentityUser user, string? userName, CancellationToken cancellationToken = default)
        {
            user.UserName = userName;
            return Task.CompletedTask;
        }

        public Task SetNormalizedUserNameAsync(IdentityUser user, string? normalizedName, CancellationToken cancellationToken = default)
        {
            user.NormalizedUserName = normalizedName;
            return Task.CompletedTask;
        }

        public Task<IdentityResult> CreateAsync(IdentityUser user, CancellationToken cancellationToken = default)
            => Task.FromResult(IdentityResult.Success);

        public Task<IdentityResult> UpdateAsync(IdentityUser user, CancellationToken cancellationToken = default)
            => Task.FromResult(IdentityResult.Success);

        public Task<IdentityResult> DeleteAsync(IdentityUser user, CancellationToken cancellationToken = default)
            => Task.FromResult(IdentityResult.Success);
    }
}

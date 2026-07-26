using Microsoft.AspNetCore.Identity;
using Site.Identity;
using SiteTests.Helpers;
using Xunit;

namespace SiteTests.Identity;

public class UserStoreTest
{
    private readonly FakeKnownLoginEmailService _knownEmails = new();
    private readonly UserStore _store;

    public UserStoreTest()
    {
        _knownEmails.KnownEmails.Add("user@test.com");
        _store = new UserStore(_knownEmails);
    }

    [Fact]
    public async Task GetUserIdAsync_ReturnsEmail()
    {
        var user = new IdentityUser { Email = "test@example.com" };
        var result = await _store.GetUserIdAsync(user, CancellationToken.None);
        Assert.Equal("test@example.com", result);
    }

    [Fact]
    public async Task GetUserIdAsync_ThrowsWhenEmailIsNull()
    {
        var user = new IdentityUser { Email = null };
        await Assert.ThrowsAsync<Exception>(
            () => _store.GetUserIdAsync(user, CancellationToken.None));
    }

    [Fact]
    public async Task FindByEmailAsync_KnownEmail_ReturnsUserWithEmail()
    {
        var result = await _store.FindByEmailAsync("user@test.com", CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal("user@test.com", result!.Email);
    }

    [Fact]
    public async Task FindByEmailAsync_UnknownEmail_ReturnsNull()
    {
        var result = await _store.FindByEmailAsync("stranger@example.com", CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public async Task FindByEmailAsync_AdminEmail_ReturnsUser()
    {
        _knownEmails.AdminEmails.Add("admin@test.com");

        var result = await _store.FindByEmailAsync("admin@test.com", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("admin@test.com", result!.Email);
    }

    [Fact]
    public async Task FindByEmailAsync_EmptyEmail_ReturnsNull()
    {
        var result = await _store.FindByEmailAsync("", CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var store = new UserStore(_knownEmails);
        store.Dispose(); // Should not throw
    }

    [Fact]
    public async Task GetUserNameAsync_ThrowsNotImplementedException()
    {
        await Assert.ThrowsAsync<NotImplementedException>(
            () => _store.GetUserNameAsync(new IdentityUser(), CancellationToken.None));
    }

    [Fact]
    public async Task SetUserNameAsync_ThrowsNotImplementedException()
    {
        await Assert.ThrowsAsync<NotImplementedException>(
            () => _store.SetUserNameAsync(new IdentityUser(), "name", CancellationToken.None));
    }

    [Fact]
    public async Task GetNormalizedUserNameAsync_ThrowsNotImplementedException()
    {
        await Assert.ThrowsAsync<NotImplementedException>(
            () => _store.GetNormalizedUserNameAsync(new IdentityUser(), CancellationToken.None));
    }

    [Fact]
    public async Task SetNormalizedUserNameAsync_ThrowsNotImplementedException()
    {
        await Assert.ThrowsAsync<NotImplementedException>(
            () => _store.SetNormalizedUserNameAsync(new IdentityUser(), "name", CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_ThrowsNotImplementedException()
    {
        await Assert.ThrowsAsync<NotImplementedException>(
            () => _store.CreateAsync(new IdentityUser(), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_ThrowsNotImplementedException()
    {
        await Assert.ThrowsAsync<NotImplementedException>(
            () => _store.UpdateAsync(new IdentityUser(), CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAsync_ThrowsNotImplementedException()
    {
        await Assert.ThrowsAsync<NotImplementedException>(
            () => _store.DeleteAsync(new IdentityUser(), CancellationToken.None));
    }

    [Fact]
    public async Task FindByIdAsync_ThrowsNotImplementedException()
    {
        await Assert.ThrowsAsync<NotImplementedException>(
            () => _store.FindByIdAsync("id", CancellationToken.None));
    }

    [Fact]
    public async Task FindByNameAsync_ThrowsNotImplementedException()
    {
        await Assert.ThrowsAsync<NotImplementedException>(
            () => _store.FindByNameAsync("name", CancellationToken.None));
    }

    [Fact]
    public async Task SetEmailAsync_ThrowsNotImplementedException()
    {
        await Assert.ThrowsAsync<NotImplementedException>(
            () => _store.SetEmailAsync(new IdentityUser(), "e@e.com", CancellationToken.None));
    }

    [Fact]
    public async Task GetEmailAsync_ThrowsNotImplementedException()
    {
        await Assert.ThrowsAsync<NotImplementedException>(
            () => _store.GetEmailAsync(new IdentityUser(), CancellationToken.None));
    }

    [Fact]
    public async Task GetEmailConfirmedAsync_ThrowsNotImplementedException()
    {
        await Assert.ThrowsAsync<NotImplementedException>(
            () => _store.GetEmailConfirmedAsync(new IdentityUser(), CancellationToken.None));
    }

    [Fact]
    public async Task SetEmailConfirmedAsync_ThrowsNotImplementedException()
    {
        await Assert.ThrowsAsync<NotImplementedException>(
            () => _store.SetEmailConfirmedAsync(new IdentityUser(), true, CancellationToken.None));
    }

    [Fact]
    public async Task GetNormalizedEmailAsync_ThrowsNotImplementedException()
    {
        await Assert.ThrowsAsync<NotImplementedException>(
            () => _store.GetNormalizedEmailAsync(new IdentityUser(), CancellationToken.None));
    }

    [Fact]
    public async Task SetNormalizedEmailAsync_ThrowsNotImplementedException()
    {
        await Assert.ThrowsAsync<NotImplementedException>(
            () => _store.SetNormalizedEmailAsync(new IdentityUser(), "name", CancellationToken.None));
    }
}

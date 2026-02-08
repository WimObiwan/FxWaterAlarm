using Microsoft.AspNetCore.Identity;
using Site.Identity;
using Xunit;

namespace SiteTests.Identity;

public class RoleStoreTest
{
    private readonly RoleStore _store = new();

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var store = new RoleStore();
        store.Dispose(); // Should not throw
    }

    [Fact]
    public async Task CreateAsync_ThrowsNotImplementedException()
    {
        await Assert.ThrowsAsync<NotImplementedException>(
            () => _store.CreateAsync(new IdentityRole(), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_ThrowsNotImplementedException()
    {
        await Assert.ThrowsAsync<NotImplementedException>(
            () => _store.UpdateAsync(new IdentityRole(), CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAsync_ThrowsNotImplementedException()
    {
        await Assert.ThrowsAsync<NotImplementedException>(
            () => _store.DeleteAsync(new IdentityRole(), CancellationToken.None));
    }

    [Fact]
    public async Task GetRoleIdAsync_ThrowsNotImplementedException()
    {
        await Assert.ThrowsAsync<NotImplementedException>(
            () => _store.GetRoleIdAsync(new IdentityRole(), CancellationToken.None));
    }

    [Fact]
    public async Task GetRoleNameAsync_ThrowsNotImplementedException()
    {
        await Assert.ThrowsAsync<NotImplementedException>(
            () => _store.GetRoleNameAsync(new IdentityRole(), CancellationToken.None));
    }

    [Fact]
    public async Task SetRoleNameAsync_ThrowsNotImplementedException()
    {
        await Assert.ThrowsAsync<NotImplementedException>(
            () => _store.SetRoleNameAsync(new IdentityRole(), "name", CancellationToken.None));
    }

    [Fact]
    public async Task GetNormalizedRoleNameAsync_ThrowsNotImplementedException()
    {
        await Assert.ThrowsAsync<NotImplementedException>(
            () => _store.GetNormalizedRoleNameAsync(new IdentityRole(), CancellationToken.None));
    }

    [Fact]
    public async Task SetNormalizedRoleNameAsync_ThrowsNotImplementedException()
    {
        await Assert.ThrowsAsync<NotImplementedException>(
            () => _store.SetNormalizedRoleNameAsync(new IdentityRole(), "name", CancellationToken.None));
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
}

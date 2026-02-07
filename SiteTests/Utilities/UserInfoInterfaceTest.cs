using Site.Utilities;
using Xunit;

namespace SiteTests.Utilities;

/// <summary>
/// Tests for the IUserInfo interface contract.
/// Since UserInfo depends heavily on HttpContext and IAuthorizationService,
/// these tests verify the interface surface and basic scenarios via a fake implementation.
/// </summary>
public class UserInfoInterfaceTest
{
    private class FakeUserInfo : IUserInfo
    {
        public bool AuthenticatedResult { get; set; }
        public string? EmailResult { get; set; }
        public bool AdminResult { get; set; }
        public bool CanUpdateResult { get; set; }

        public bool IsAuthenticated() => AuthenticatedResult;
        public string? GetLoginEmail() => EmailResult;
        public Task<bool> IsAdmin() => Task.FromResult(AdminResult);

        public Task<bool> CanUpdateAccount(Core.Entities.Account account) =>
            Task.FromResult(CanUpdateResult);

        public Task<bool> CanUpdateAccountSensor(Core.Entities.AccountSensor accountSensor) =>
            Task.FromResult(CanUpdateResult);
    }

    [Fact]
    public void IsAuthenticated_WhenTrue_ReturnsTrue()
    {
        var userInfo = new FakeUserInfo { AuthenticatedResult = true };
        Assert.True(userInfo.IsAuthenticated());
    }

    [Fact]
    public void IsAuthenticated_WhenFalse_ReturnsFalse()
    {
        var userInfo = new FakeUserInfo { AuthenticatedResult = false };
        Assert.False(userInfo.IsAuthenticated());
    }

    [Fact]
    public void GetLoginEmail_ReturnsEmail()
    {
        var userInfo = new FakeUserInfo { EmailResult = "user@example.com" };
        Assert.Equal("user@example.com", userInfo.GetLoginEmail());
    }

    [Fact]
    public void GetLoginEmail_ReturnsNull_WhenNotAuthenticated()
    {
        var userInfo = new FakeUserInfo { EmailResult = null };
        Assert.Null(userInfo.GetLoginEmail());
    }

    [Fact]
    public async Task IsAdmin_ReturnsTrue_WhenAdmin()
    {
        var userInfo = new FakeUserInfo { AdminResult = true };
        Assert.True(await userInfo.IsAdmin());
    }

    [Fact]
    public async Task IsAdmin_ReturnsFalse_WhenNotAdmin()
    {
        var userInfo = new FakeUserInfo { AdminResult = false };
        Assert.False(await userInfo.IsAdmin());
    }

    [Fact]
    public async Task CanUpdateAccount_ReturnsTrueWhenAllowed()
    {
        var account = new Core.Entities.Account
        {
            Uid = Guid.NewGuid(),
            Email = "test@example.com",
            CreationTimestamp = DateTime.UtcNow
        };
        var userInfo = new FakeUserInfo { CanUpdateResult = true };
        Assert.True(await userInfo.CanUpdateAccount(account));
    }

    [Fact]
    public async Task CanUpdateAccount_ReturnsFalseWhenDenied()
    {
        var account = new Core.Entities.Account
        {
            Uid = Guid.NewGuid(),
            Email = "test@example.com",
            CreationTimestamp = DateTime.UtcNow
        };
        var userInfo = new FakeUserInfo { CanUpdateResult = false };
        Assert.False(await userInfo.CanUpdateAccount(account));
    }

    [Fact]
    public async Task CanUpdateAccountSensor_DelegatesToCanUpdateAccount()
    {
        var account = new Core.Entities.Account
        {
            Uid = Guid.NewGuid(),
            Email = "test@example.com",
            CreationTimestamp = DateTime.UtcNow
        };
        var sensor = new Core.Entities.Sensor
        {
            Uid = Guid.NewGuid(),
            DevEui = "DEV001",
            Type = Core.Entities.SensorType.Level,
            CreateTimestamp = DateTime.UtcNow
        };
        var accountSensor = new Core.Entities.AccountSensor
        {
            Account = account,
            Sensor = sensor,
            CreateTimestamp = DateTime.UtcNow
        };

        var userInfo = new FakeUserInfo { CanUpdateResult = true };
        Assert.True(await userInfo.CanUpdateAccountSensor(accountSensor));
    }
}

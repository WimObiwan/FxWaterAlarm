using Core.Entities;
using Core.Util;
using System.Reflection;
using Xunit;

namespace CoreTests.Util;

public class ResetAccountSensorOrderHelperTest
{
    private Account CreateAccountWithSensors(params int[] orders)
    {
        var account = new Account
        {
            Uid = Guid.NewGuid(),
            Email = "test@example.com",
            CreationTimestamp = DateTime.UtcNow
        };

        // Initialize the private _accountSensors field
        var accountSensorsField = typeof(Account).GetField("_accountSensors", BindingFlags.NonPublic | BindingFlags.Instance);
        var accountSensors = new List<AccountSensor>();
        accountSensorsField?.SetValue(account, accountSensors);

        foreach (var order in orders)
        {
            var sensor = new Sensor
            {
                Uid = Guid.NewGuid(),
                DevEui = $"dev{order}",
                CreateTimestamp = DateTime.UtcNow,
                Type = SensorType.Level
            };
            accountSensors.Add(new AccountSensor
            {
                Sensor = sensor,
                Account = account,
                CreateTimestamp = DateTime.UtcNow,
                Order = order
            });
        }

        return account;
    }

    [Fact]
    public void ResetOrder_AlreadySequential_ReturnsFalse()
    {
        var account = CreateAccountWithSensors(0, 1, 2);

        var changed = InvokeResetOrder(account);

        Assert.False(changed);
        Assert.Equal([0, 1, 2], account.AccountSensors.Select(a => a.Order).ToArray());
    }

    [Fact]
    public void ResetOrder_WithGaps_ReturnsTrue()
    {
        var account = CreateAccountWithSensors(0, 5, 10);

        var changed = InvokeResetOrder(account);

        Assert.True(changed);
        Assert.Equal([0, 1, 2], account.AccountSensors.Select(a => a.Order).ToArray());
    }

    [Fact]
    public void ResetOrder_SingleSensor_AlreadyZero_ReturnsFalse()
    {
        var account = CreateAccountWithSensors(0);

        var changed = InvokeResetOrder(account);

        Assert.False(changed);
        Assert.Equal([0], account.AccountSensors.Select(a => a.Order).ToArray());
    }

    [Fact]
    public void ResetOrder_SingleSensor_NotZero_ReturnsTrue()
    {
        var account = CreateAccountWithSensors(5);

        var changed = InvokeResetOrder(account);

        Assert.True(changed);
        Assert.Equal([0], account.AccountSensors.Select(a => a.Order).ToArray());
    }

    [Fact]
    public void ResetOrder_Empty_ReturnsFalse()
    {
        var account = CreateAccountWithSensors();

        var changed = InvokeResetOrder(account);

        Assert.False(changed);
        Assert.Empty(account.AccountSensors);
    }

    [Fact]
    public void ResetOrder_UnsortedOrders_SortsAndResets()
    {
        var account = CreateAccountWithSensors(3, 1, 2);

        var changed = InvokeResetOrder(account);

        Assert.True(changed);
        // After reset, the sensors should be re-ordered by their original Order (sorted: 1,2,3 -> assigned: 0,1,2)
        var orderedDevEuis = account.AccountSensors.OrderBy(a => a.Order).Select(a => a.Sensor.DevEui).ToArray();
        Assert.Equal(["dev1", "dev2", "dev3"], orderedDevEuis);
    }

    [Fact]
    public void ResetOrder_WithPreferredSensor_PutsPreferredFirst()
    {
        var account = CreateAccountWithSensors(0, 0, 0);

        // All sensors have the same order; the preferred one should come first due to ThenBy
        var preferredSensor = account.AccountSensors.Last();

        var changed = InvokeResetOrder(account, preferredSensor);

        Assert.True(changed);
        // The preferred sensor should get order 0 (first in tie-breaking)
        Assert.Equal(0, preferredSensor.Order);
    }

    [Fact]
    public void ResetOrder_PreservesSortOrder_WhenNoGaps()
    {
        var account = CreateAccountWithSensors(0, 1, 2);

        // Call with a preferred sensor that is already first - no change
        var preferredSensor = account.AccountSensors.First();
        var changed = InvokeResetOrder(account, preferredSensor);

        Assert.False(changed);
    }

    private static bool InvokeResetOrder(Account account, AccountSensor? preferredSensor = null)
    {
        // ResetAccountSensorOrderHelper is internal but accessible via InternalsVisibleTo
        return ResetAccountSensorOrderHelper.ResetOrder(null, account, preferredSensor);
    }
}

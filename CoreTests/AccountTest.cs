using Core.Entities;
using Core.Exceptions;
using System.Reflection;
using Xunit;

namespace CoreTests;

public class AccountTest
{
    private static Account CreateAccount(string email = "test@example.com", string? link = "abc123")
    {
        var account = new Account
        {
            Uid = Guid.NewGuid(),
            Email = email,
            CreationTimestamp = DateTime.UtcNow,
            Link = link
        };

        // Initialize private backing fields for testing
        var accountSensorsField = typeof(Account).GetField("_accountSensors", BindingFlags.NonPublic | BindingFlags.Instance);
        accountSensorsField?.SetValue(account, new List<AccountSensor>());

        var sensorsField = typeof(Account).GetField("_sensors", BindingFlags.NonPublic | BindingFlags.Instance);
        sensorsField?.SetValue(account, new List<Sensor>());

        return account;
    }

    private static Sensor CreateSensor(SensorType type = SensorType.Level, string? link = "sensor1")
    {
        return new Sensor
        {
            Uid = Guid.NewGuid(),
            DevEui = "dev123",
            CreateTimestamp = DateTime.UtcNow,
            Type = type,
            Link = link
        };
    }

    // --- RestPath ---

    [Fact]
    public void RestPath_WithLink_ReturnsPath()
    {
        var account = CreateAccount(link: "mylink");
        Assert.Equal("/a/mylink", account.RestPath);
    }

    [Fact]
    public void RestPath_WithoutLink_ReturnsNull()
    {
        var account = CreateAccount(link: null);
        Assert.Null(account.RestPath);
    }

    // --- IsDemo / IsDemoEmail ---

    [Fact]
    public void IsDemo_DemoEmail_ReturnsTrue()
    {
        var account = CreateAccount(email: "demo@wateralarm.be");
        Assert.True(account.IsDemo);
    }

    [Fact]
    public void IsDemo_DemoEmailCaseInsensitive_ReturnsTrue()
    {
        var account = CreateAccount(email: "Demo@WaterAlarm.BE");
        Assert.True(account.IsDemo);
    }

    [Fact]
    public void IsDemo_RegularEmail_ReturnsFalse()
    {
        var account = CreateAccount(email: "user@example.com");
        Assert.False(account.IsDemo);
    }

    [Fact]
    public void IsDemoEmail_Static_DemoEmail()
    {
        Assert.True(Account.IsDemoEmail("demo@wateralarm.be"));
    }

    [Fact]
    public void IsDemoEmail_Static_NotDemoEmail()
    {
        Assert.False(Account.IsDemoEmail("other@example.com"));
    }

    // --- AppPath ---

    [Fact]
    public void AppPath_DemoAccount_ReturnsNull()
    {
        var account = CreateAccount(email: "demo@wateralarm.be", link: "abc");
        Assert.Null(account.AppPath);
    }

    [Fact]
    public void AppPath_SingleSensor_ReturnsSensorRestPath()
    {
        var account = CreateAccount(email: "user@example.com", link: "acclink");
        var sensor = CreateSensor(link: "slink");

        account.AddSensor(sensor);

        // Single sensor: AppPath should be the sensor's RestPath
        var accountSensor = account.AccountSensors.Single();
        Assert.Equal(accountSensor.RestPath, account.AppPath);
    }

    [Fact]
    public void AppPath_MultipleSensors_ReturnsAccountRestPath()
    {
        var account = CreateAccount(email: "user@example.com", link: "acclink");
        account.AddSensor(CreateSensor(link: "slink1"));
        account.AddSensor(CreateSensor(link: "slink2"));

        Assert.Equal("/a/acclink", account.AppPath);
    }

    [Fact]
    public void AppPath_NoSensors_ReturnsAccountRestPath()
    {
        var account = CreateAccount(email: "user@example.com", link: "acclink");

        // 0 sensors: _accountSensors.Count == 0, not == 1, so falls through to RestPath
        Assert.Equal("/a/acclink", account.AppPath);
    }

    // --- AddSensor ---

    [Fact]
    public void AddSensor_FirstSensor_OrderIsZero()
    {
        var account = CreateAccount();
        var sensor = CreateSensor();

        account.AddSensor(sensor);

        Assert.Single(account.AccountSensors);
        Assert.Equal(0, account.AccountSensors.First().Order);
    }

    [Fact]
    public void AddSensor_MultipleSensors_IncrementOrder()
    {
        var account = CreateAccount();
        account.AddSensor(CreateSensor(link: "s1"));
        account.AddSensor(CreateSensor(link: "s2"));
        account.AddSensor(CreateSensor(link: "s3"));

        var orders = account.AccountSensors.Select(a => a.Order).ToList();
        Assert.Equal([0, 1, 2], orders);
    }

    // --- RemoveSensor ---

    [Fact]
    public void RemoveSensor_ExistingSensor_ReturnsTrue()
    {
        var account = CreateAccount();
        var sensorsField = typeof(Account).GetField("_sensors", BindingFlags.NonPublic | BindingFlags.Instance);
        var sensors = (List<Sensor>)sensorsField!.GetValue(account)!;

        var sensor = CreateSensor();
        sensors.Add(sensor);

        Assert.True(account.RemoveSensor(sensor));
        Assert.Empty(account.Sensors);
    }

    [Fact]
    public void RemoveSensor_NonexistentSensor_ReturnsFalse()
    {
        var account = CreateAccount();
        var sensor = CreateSensor();

        Assert.False(account.RemoveSensor(sensor));
    }
}

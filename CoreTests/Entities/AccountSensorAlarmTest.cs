using Core.Entities;
using System.Reflection;
using Xunit;

namespace CoreTests.Entities;

public class AccountSensorAlarmTest
{
    private Account CreateAccount()
    {
        return new Account
        {
            Uid = Guid.NewGuid(),
            Email = "test@example.com",
            CreationTimestamp = DateTime.UtcNow
        };
    }

    private Sensor CreateSensor(SensorType type)
    {
        return new Sensor
        {
            Uid = Guid.NewGuid(),
            DevEui = "dev123",
            CreateTimestamp = DateTime.UtcNow,
            Type = type
        };
    }

    private AccountSensor CreateAccountSensor(SensorType type)
    {
        var accountSensor = new AccountSensor
        {
            Sensor = CreateSensor(type),
            DistanceMmEmpty = 3000,
            DistanceMmFull = 800,
            UnusableHeightMm = 200,
            CapacityL = 10000,
            CreateTimestamp = DateTime.UtcNow,
            Account = CreateAccount()
        };

        // Initialize the private _alarms field for testing
        var alarmsField = typeof(AccountSensor).GetField("_alarms", BindingFlags.NonPublic | BindingFlags.Instance);
        alarmsField?.SetValue(accountSensor, new List<AccountSensorAlarm>());

        return accountSensor;
    }

    [Fact]
    public void TestAddAlarm()
    {
        var accountSensor = CreateAccountSensor(SensorType.Level);
        var alarm = new AccountSensorAlarm
        {
            Uid = Guid.NewGuid(),
            AlarmType = AccountSensorAlarmType.PercentageLow,
            AlarmThreshold = 10.0
        };

        accountSensor.AddAlarm(alarm);

        Assert.Single(accountSensor.Alarms);
        Assert.Equal(AccountSensorAlarmType.PercentageLow, accountSensor.Alarms.First().AlarmType);
        Assert.Equal(10.0, accountSensor.Alarms.First().AlarmThreshold);
    }

    [Fact]
    public void TestRemoveAlarm()
    {
        var accountSensor = CreateAccountSensor(SensorType.Level);
        var alarm = new AccountSensorAlarm
        {
            Uid = Guid.NewGuid(),
            AlarmType = AccountSensorAlarmType.Battery,
            AlarmThreshold = 3.5
        };

        accountSensor.AddAlarm(alarm);
        Assert.Single(accountSensor.Alarms);

        var removed = accountSensor.RemoveAlarm(alarm);
        Assert.True(removed);
        Assert.Empty(accountSensor.Alarms);
    }

    [Fact]
    public void TestAlarmCurrentlyTriggered()
    {
        var alarm = new AccountSensorAlarm
        {
            Uid = Guid.NewGuid(),
            AlarmType = AccountSensorAlarmType.DetectOn,
            LastTriggered = DateTime.UtcNow,
            LastCleared = null
        };

        Assert.True(alarm.IsCurrentlyTriggered);
        Assert.False(alarm.IsCurrentlyCleared);
    }

    [Fact]
    public void TestAlarmCurrentlyCleared()
    {
        var alarm = new AccountSensorAlarm
        {
            Uid = Guid.NewGuid(),
            AlarmType = AccountSensorAlarmType.DetectOn,
            LastTriggered = DateTime.UtcNow.AddMinutes(-10),
            LastCleared = DateTime.UtcNow
        };

        Assert.False(alarm.IsCurrentlyTriggered);
        Assert.True(alarm.IsCurrentlyCleared);
    }

    [Fact]
    public void TestMultipleAlarmsOfDifferentTypes()
    {
        var accountSensor = CreateAccountSensor(SensorType.Level);
        var batteryAlarm = new AccountSensorAlarm
        {
            Uid = Guid.NewGuid(),
            AlarmType = AccountSensorAlarmType.Battery,
            AlarmThreshold = 3.5
        };
        var percentageAlarm = new AccountSensorAlarm
        {
            Uid = Guid.NewGuid(),
            AlarmType = AccountSensorAlarmType.PercentageLow,
            AlarmThreshold = 20.0
        };

        accountSensor.AddAlarm(batteryAlarm);
        accountSensor.AddAlarm(percentageAlarm);

        Assert.Equal(2, accountSensor.Alarms.Count);
        Assert.Contains(accountSensor.Alarms, a => a.AlarmType == AccountSensorAlarmType.Battery);
        Assert.Contains(accountSensor.Alarms, a => a.AlarmType == AccountSensorAlarmType.PercentageLow);
    }

    [Fact]
    public void IsCurrentlyTriggered_NeverTriggered_ReturnsFalse()
    {
        var alarm = new AccountSensorAlarm
        {
            Uid = Guid.NewGuid(),
            AlarmType = AccountSensorAlarmType.Data,
            LastTriggered = null,
            LastCleared = null
        };
        Assert.False(alarm.IsCurrentlyTriggered);
    }

    [Fact]
    public void IsCurrentlyCleared_NeverCleared_ReturnsFalse()
    {
        var alarm = new AccountSensorAlarm
        {
            Uid = Guid.NewGuid(),
            AlarmType = AccountSensorAlarmType.Data,
            LastTriggered = null,
            LastCleared = null
        };
        Assert.False(alarm.IsCurrentlyCleared);
    }

    [Fact]
    public void IsCurrentlyCleared_ClearedNoTriggered_ReturnsTrue()
    {
        var alarm = new AccountSensorAlarm
        {
            Uid = Guid.NewGuid(),
            AlarmType = AccountSensorAlarmType.Battery,
            LastTriggered = null,
            LastCleared = DateTime.UtcNow
        };
        Assert.True(alarm.IsCurrentlyCleared);
    }

    [Fact]
    public void IsCurrentlyTriggered_TriggeredAfterCleared_ReturnsTrue()
    {
        var alarm = new AccountSensorAlarm
        {
            Uid = Guid.NewGuid(),
            AlarmType = AccountSensorAlarmType.Data,
            LastTriggered = DateTime.UtcNow,
            LastCleared = DateTime.UtcNow.AddMinutes(-10)
        };
        Assert.True(alarm.IsCurrentlyTriggered);
    }

    [Fact]
    public void AlarmThreshold_CanBeNull()
    {
        var alarm = new AccountSensorAlarm
        {
            Uid = Guid.NewGuid(),
            AlarmType = AccountSensorAlarmType.DetectOn,
            AlarmThreshold = null
        };
        Assert.Null(alarm.AlarmThreshold);
    }
}
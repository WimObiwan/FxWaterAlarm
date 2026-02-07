using Core.Entities;
using Core.Util;
using Xunit;

namespace CoreTests.Entities;

public class AccountSensorTest
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

    private AccountSensor CreateAccountSensor(SensorType type, int? distanceMmEmpty, int? distanceMmFull, int? capacityL)
    {
        return new AccountSensor
        {
            Sensor = CreateSensor(type),
            DistanceMmEmpty = distanceMmEmpty,
            DistanceMmFull = distanceMmFull,
            UnusableHeightMm = 200,
            CapacityL = capacityL,
            CreateTimestamp = DateTime.UtcNow,
            Account = CreateAccount()
        };
    }

    [Fact]
    public void TestGraphsLevelNormal()
    {
        var accountSensor = CreateAccountSensor(SensorType.Level,
            capacityL: 10000, distanceMmFull: 800, distanceMmEmpty: 3000);

        // Assert
        Assert.True(accountSensor.HasDistance);
        Assert.True(accountSensor.HasHeight);
        Assert.True(accountSensor.HasVolume);
        Assert.True(accountSensor.HasPercentage);
        Assert.False(accountSensor.HasConductivity);
        Assert.False(accountSensor.HasStatus);
        Assert.False(accountSensor.HasTemperature);
    }

    [Fact]
    public void TestGraphsLevelNoCapacity()
    {
        var accountSensor = CreateAccountSensor(SensorType.Level,
            capacityL: null, distanceMmFull: 800, distanceMmEmpty: 3000);

        // Assert
        Assert.True(accountSensor.HasDistance);
        Assert.True(accountSensor.HasHeight);
        Assert.False(accountSensor.HasVolume);
        Assert.True(accountSensor.HasPercentage);
        Assert.False(accountSensor.HasConductivity);
        Assert.False(accountSensor.HasStatus);
        Assert.False(accountSensor.HasTemperature);
    }

    [Fact]
    public void TestGraphsLevelNoFull()
    {
        var accountSensor = CreateAccountSensor(SensorType.Level,
            capacityL: null, distanceMmFull: null, distanceMmEmpty: 3000);

        // Assert
        Assert.True(accountSensor.HasDistance);
        Assert.True(accountSensor.HasHeight);
        Assert.False(accountSensor.HasVolume);
        Assert.False(accountSensor.HasPercentage);
        Assert.False(accountSensor.HasConductivity);
        Assert.False(accountSensor.HasStatus);
        Assert.False(accountSensor.HasTemperature);
    }

    [Fact]
    public void TestGraphsLevelNoEmpty()
    {
        var accountSensor = CreateAccountSensor(SensorType.Level,
            capacityL: null, distanceMmFull: null, distanceMmEmpty: null);

        // Assert
        Assert.True(accountSensor.HasDistance);
        Assert.False(accountSensor.HasHeight);
        Assert.False(accountSensor.HasVolume);
        Assert.False(accountSensor.HasPercentage);
        Assert.False(accountSensor.HasConductivity);
        Assert.False(accountSensor.HasStatus);
        Assert.False(accountSensor.HasTemperature);
    }

    [Fact]
    public void TestGraphsLevelPressureNormal()
    {
        var accountSensor = CreateAccountSensor(SensorType.LevelPressure,
            capacityL: 10000, distanceMmFull: 1900, distanceMmEmpty: 100);

        // Assert
        Assert.False(accountSensor.HasDistance);
        Assert.True(accountSensor.HasHeight);
        Assert.True(accountSensor.HasVolume);
        Assert.True(accountSensor.HasPercentage);
        Assert.False(accountSensor.HasConductivity);
        Assert.False(accountSensor.HasStatus);
        Assert.False(accountSensor.HasTemperature);
    }

    [Fact]
    public void TestGraphsLevelPressureNoEmpty()
    {
        var accountSensor = CreateAccountSensor(SensorType.LevelPressure,
            capacityL: 10000, distanceMmFull: 1900, distanceMmEmpty: null);

        // Assert
        Assert.False(accountSensor.HasDistance);
        Assert.True(accountSensor.HasHeight);
        Assert.True(accountSensor.HasVolume);
        Assert.True(accountSensor.HasPercentage);
        Assert.False(accountSensor.HasConductivity);
        Assert.False(accountSensor.HasStatus);
        Assert.False(accountSensor.HasTemperature);
    }

    [Fact]
    public void TestGraphsLevelPressureNoCapacity()
    {
        var accountSensor = CreateAccountSensor(SensorType.LevelPressure,
            capacityL: null, distanceMmFull: 1900, distanceMmEmpty: 100);

        // Assert
        Assert.False(accountSensor.HasDistance);
        Assert.True(accountSensor.HasHeight);
        Assert.False(accountSensor.HasVolume);
        Assert.True(accountSensor.HasPercentage);
        Assert.False(accountSensor.HasConductivity);
        Assert.False(accountSensor.HasStatus);
        Assert.False(accountSensor.HasTemperature);
    }

    [Fact]
    public void TestGraphsLevelPressureNoFull()
    {
        var accountSensor = CreateAccountSensor(SensorType.LevelPressure,
            capacityL: null, distanceMmFull: null, distanceMmEmpty: 100);

        // Assert
        Assert.False(accountSensor.HasDistance);
        Assert.True(accountSensor.HasHeight);
        Assert.False(accountSensor.HasVolume);
        Assert.False(accountSensor.HasPercentage);
        Assert.False(accountSensor.HasConductivity);
        Assert.False(accountSensor.HasStatus);
        Assert.False(accountSensor.HasTemperature);
    }
}

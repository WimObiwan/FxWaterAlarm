using Core.Entities;
using Core.Util;
using Site.Utilities;
using Xunit;

namespace CoreTests;

public class MeasurementDisplayExtensionsTest
{
    private AccountSensor CreateTestAccountSensor()
    {
        var account = new Account
        {
            Uid = Guid.NewGuid(),
            Email = "test@example.com",
            CreationTimestamp = DateTime.UtcNow
        };

        var sensor = new Sensor
        {
            Uid = Guid.NewGuid(),
            DevEui = "dev123",
            Type = SensorType.Level,
            CreateTimestamp = DateTime.UtcNow
        };

        return new AccountSensor
        {
            Sensor = sensor,
            Account = account,
            CreateTimestamp = DateTime.UtcNow
        };
    }

    [Fact]
    public void TestMeasurementIsOld_WhenOlderThanThreshold_ReturnsTrue()
    {
        // Arrange
        var oldTimestamp = DateTime.UtcNow.AddHours(-25); // 25 hours ago
        var threshold = TimeSpan.FromHours(24); // 24 hour threshold
        
        var measurement = new MeasurementLevel
        {
            DevEui = "dev123",
            Timestamp = oldTimestamp,
            BatV = 3.7,
            RssiDbm = -80,
            DistanceMm = 1200
        };
        
        var accountSensor = CreateTestAccountSensor();
        var measurementEx = new MeasurementLevelEx(measurement, accountSensor);

        // Act
        var isOld = measurementEx.IsOld(threshold);

        // Assert
        Assert.True(isOld);
    }

    [Fact]
    public void TestMeasurementIsOld_WhenNewerThanThreshold_ReturnsFalse()
    {
        // Arrange
        var recentTimestamp = DateTime.UtcNow.AddHours(-12); // 12 hours ago
        var threshold = TimeSpan.FromHours(24); // 24 hour threshold
        
        var measurement = new MeasurementLevel
        {
            DevEui = "dev123",
            Timestamp = recentTimestamp,
            BatV = 3.7,
            RssiDbm = -80,
            DistanceMm = 1200
        };
        
        var accountSensor = CreateTestAccountSensor();
        var measurementEx = new MeasurementLevelEx(measurement, accountSensor);

        // Act
        var isOld = measurementEx.IsOld(threshold);

        // Assert
        Assert.False(isOld);
    }

    [Fact]
    public void TestMeasurementIsOld_WhenExactlyAtThreshold_ReturnsTrue()
    {
        // Arrange
        var threshold = TimeSpan.FromHours(24);
        var exactlyAtThreshold = DateTime.UtcNow.Add(-threshold);
        
        var measurement = new MeasurementLevel
        {
            DevEui = "dev123",
            Timestamp = exactlyAtThreshold,
            BatV = 3.7,
            RssiDbm = -80,
            DistanceMm = 1200
        };
        
        var accountSensor = CreateTestAccountSensor();
        var measurementEx = new MeasurementLevelEx(measurement, accountSensor);

        // Act
        var isOld = measurementEx.IsOld(threshold);

        // Assert
        Assert.True(isOld);
    }

    [Fact]
    public void TestEstimateNextRefresh_UsesExpectedIntervalSecs()
    {
        // Arrange
        var customInterval = 1800; // 30 minutes in seconds
        var measurementTimestamp = DateTime.UtcNow.AddMinutes(-15); // 15 minutes ago
        
        var account = new Account
        {
            Uid = Guid.NewGuid(),
            Email = "test@example.com",
            CreationTimestamp = DateTime.UtcNow
        };

        var sensor = new Sensor
        {
            Uid = Guid.NewGuid(),
            DevEui = "dev123",
            Type = SensorType.Level,
            CreateTimestamp = DateTime.UtcNow,
            ExpectedIntervalSecs = customInterval
        };

        var accountSensor = new AccountSensor
        {
            Sensor = sensor,
            Account = account,
            CreateTimestamp = DateTime.UtcNow
        };
        
        var measurement = new MeasurementLevel
        {
            DevEui = "dev123",
            Timestamp = measurementTimestamp,
            BatV = 3.7,
            RssiDbm = -80,
            DistanceMm = 1200
        };
        
        var measurementEx = new MeasurementLevelEx(measurement, accountSensor);

        // Act
        var nextRefresh = measurementEx.EstimateNextRefresh();

        // Assert
        // The next refresh should be calculated based on the custom interval
        var expectedNextRefresh = measurementTimestamp.AddSeconds(customInterval + 5);
        var tolerance = TimeSpan.FromSeconds(5);
        
        Assert.True(Math.Abs((nextRefresh - expectedNextRefresh).TotalSeconds) <= tolerance.TotalSeconds,
            $"Expected next refresh around {expectedNextRefresh}, but got {nextRefresh}. Difference: {(nextRefresh - expectedNextRefresh).TotalSeconds} seconds");
    }
}
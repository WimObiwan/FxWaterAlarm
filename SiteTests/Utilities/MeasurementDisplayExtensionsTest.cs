using Core.Entities;
using Core.Util;
using Site.Utilities;
using Xunit;

namespace SiteTests.Utilities;

public class MeasurementDisplayExtensionsTest
{
    private static AccountSensor CreateAccountSensor(int? expectedIntervalSecs = 3600)
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
            DevEui = "DEV001",
            Type = SensorType.Level,
            CreateTimestamp = DateTime.UtcNow,
            ExpectedIntervalSecs = expectedIntervalSecs
        };
        return new AccountSensor
        {
            Account = account,
            Sensor = sensor,
            CreateTimestamp = DateTime.UtcNow
        };
    }

    [Fact]
    public void IsOld_NullThreshold_ReturnsFalse()
    {
        var measurement = new MeasurementLevel
        {
            DevEui = "DEV001",
            Timestamp = DateTime.UtcNow.AddDays(-100),
            DistanceMm = 1000
        };
        var accountSensor = CreateAccountSensor();
        var ex = new MeasurementLevelEx(measurement, accountSensor);

        Assert.False(ex.IsOld(null));
    }

    [Fact]
    public void IsOld_RecentMeasurement_ReturnsFalse()
    {
        var measurement = new MeasurementLevel
        {
            DevEui = "DEV001",
            Timestamp = DateTime.UtcNow.AddMinutes(-10),
            DistanceMm = 1000
        };
        var accountSensor = CreateAccountSensor(expectedIntervalSecs: 3600);
        var ex = new MeasurementLevelEx(measurement, accountSensor);

        // threshold=1, interval=3600s => threshold = (3600+30)*1 = 3630s = ~60.5 min
        // 10 min old < 60.5 min => not old
        Assert.False(ex.IsOld(1));
    }

    [Fact]
    public void IsOld_OldMeasurement_ReturnsTrue()
    {
        var measurement = new MeasurementLevel
        {
            DevEui = "DEV001",
            Timestamp = DateTime.UtcNow.AddHours(-3),
            DistanceMm = 1000
        };
        var accountSensor = CreateAccountSensor(expectedIntervalSecs: 3600);
        var ex = new MeasurementLevelEx(measurement, accountSensor);

        // threshold=1, interval=3600s => threshold = (3600+30)*1 = 3630s = ~60.5 min
        // 3 hours old > 60.5 min => old
        Assert.True(ex.IsOld(1));
    }

    [Fact]
    public void IsOld_UsesDefaultInterval_WhenNull()
    {
        var measurement = new MeasurementLevel
        {
            DevEui = "DEV001",
            Timestamp = DateTime.UtcNow.AddHours(-5),
            DistanceMm = 1000
        };
        var accountSensor = CreateAccountSensor(expectedIntervalSecs: null);
        var ex = new MeasurementLevelEx(measurement, accountSensor);

        // null interval => default 7200s, threshold=1 => (7200+30)*1 = 7230s = ~2 hours
        // 5 hours old > 2 hours => old
        Assert.True(ex.IsOld(1));
    }

    [Fact]
    public void IsOld_MultipleIntervals_CalculatesCorrectly()
    {
        var measurement = new MeasurementLevel
        {
            DevEui = "DEV001",
            Timestamp = DateTime.UtcNow.AddHours(-5),
            DistanceMm = 1000
        };
        var accountSensor = CreateAccountSensor(expectedIntervalSecs: 3600);
        var ex = new MeasurementLevelEx(measurement, accountSensor);

        // threshold=3, interval=3600s => threshold = (3600+30)*3 = 10890s = ~3.025 hours
        // 5 hours old > 3.025 hours => old
        Assert.True(ex.IsOld(3));

        // threshold=6 => (3600+30)*6 = 21780s = ~6.05 hours
        // 5 hours old < 6.05 hours => not old
        Assert.False(ex.IsOld(6));
    }
}

using Core.Entities;
using Core.Util;
using Xunit;

namespace CoreTests.Util;

public class MeasurementExTest
{
    private AccountSensor CreateAccountSensor(int? expectedIntervalSecs = null)
    {
        var sensor = new Sensor
        {
            Uid = Guid.NewGuid(),
            DevEui = "devEx1",
            CreateTimestamp = DateTime.UtcNow,
            Type = SensorType.Level,
            ExpectedIntervalSecs = expectedIntervalSecs
        };
        var account = new Account
        {
            Uid = Guid.NewGuid(),
            Email = "ex@example.com",
            CreationTimestamp = DateTime.UtcNow
        };
        return new AccountSensor
        {
            Sensor = sensor,
            DistanceMmEmpty = 3000,
            DistanceMmFull = 800,
            Account = account,
            CreateTimestamp = DateTime.UtcNow
        };
    }

    [Fact]
    public void RssiPrc_CalculatesCorrectly()
    {
        // RssiPrc = (RssiDbm + 150.0) / 60.0 * 80.0
        var measurement = new MeasurementLevel
        {
            DevEui = "devEx1",
            Timestamp = DateTime.UtcNow,
            BatV = 3.7,
            RssiDbm = -90,
            DistanceMm = 1000
        };
        var accountSensor = CreateAccountSensor();

        var ex = new MeasurementLevelEx(measurement, accountSensor);

        // (-90 + 150) / 60 * 80 = 60 / 60 * 80 = 80
        Assert.Equal(80.0, ex.RssiPrc);
    }

    [Fact]
    public void RssiPrc_AtMinimumSignal()
    {
        var measurement = new MeasurementLevel
        {
            DevEui = "devEx1",
            Timestamp = DateTime.UtcNow,
            BatV = 3.7,
            RssiDbm = -150,
            DistanceMm = 1000
        };
        var accountSensor = CreateAccountSensor();

        var ex = new MeasurementLevelEx(measurement, accountSensor);

        // (-150 + 150) / 60 * 80 = 0
        Assert.Equal(0.0, ex.RssiPrc);
    }

    [Fact]
    public void BatteryPrc_CalculatesCorrectly()
    {
        // BatteryPrc = (BatV - 3.0) / 0.335 * 100.0
        var measurement = new MeasurementLevel
        {
            DevEui = "devEx1",
            Timestamp = DateTime.UtcNow,
            BatV = 3.335,
            RssiDbm = -80,
            DistanceMm = 1000
        };
        var accountSensor = CreateAccountSensor();

        var ex = new MeasurementLevelEx(measurement, accountSensor);

        // (3.335 - 3.0) / 0.335 * 100 = 100
        Assert.Equal(100.0, ex.BatteryPrc, 3);
    }

    [Fact]
    public void BatteryPrc_AtMinimumVoltage()
    {
        var measurement = new MeasurementLevel
        {
            DevEui = "devEx1",
            Timestamp = DateTime.UtcNow,
            BatV = 3.0,
            RssiDbm = -80,
            DistanceMm = 1000
        };
        var accountSensor = CreateAccountSensor();

        var ex = new MeasurementLevelEx(measurement, accountSensor);

        // (3.0 - 3.0) / 0.335 * 100 = 0
        Assert.Equal(0.0, ex.BatteryPrc);
    }

    [Fact]
    public void EstimateNextRefresh_UsesDefaultInterval_WhenNotSet()
    {
        var timestamp = DateTime.UtcNow.AddHours(-1);
        var measurement = new MeasurementLevel
        {
            DevEui = "devEx1",
            Timestamp = timestamp,
            BatV = 3.7,
            RssiDbm = -80,
            DistanceMm = 1000
        };
        // No ExpectedIntervalSecs → defaults to 7200
        var accountSensor = CreateAccountSensor(expectedIntervalSecs: null);

        var ex = new MeasurementLevelEx(measurement, accountSensor);
        var nextRefresh = ex.EstimateNextRefresh();

        // With 7200s interval and timestamp 1 hour ago:
        // next refresh should be at timestamp + 7200s + 5s = timestamp + 2h 0m 5s
        Assert.True(nextRefresh > DateTime.UtcNow);
        Assert.True(nextRefresh > timestamp);
    }

    [Fact]
    public void EstimateNextRefresh_UsesCustomInterval()
    {
        var timestamp = DateTime.UtcNow.AddMinutes(-30);
        var measurement = new MeasurementLevel
        {
            DevEui = "devEx1",
            Timestamp = timestamp,
            BatV = 3.7,
            RssiDbm = -80,
            DistanceMm = 1000
        };
        // 3600s = 1 hour interval
        var accountSensor = CreateAccountSensor(expectedIntervalSecs: 3600);

        var ex = new MeasurementLevelEx(measurement, accountSensor);
        var nextRefresh = ex.EstimateNextRefresh();

        // With 3600s interval and timestamp 30 minutes ago:
        // nextRefreshSecs = (1800 / 3600 + 1) * 3600 = 1 * 3600 = 3600
        // next refresh = timestamp + 3600 + 5 = timestamp + 1h 0m 5s
        var expected = timestamp.AddSeconds(3605);
        Assert.Equal(expected, nextRefresh);
    }

    [Fact]
    public void EstimateNextRefresh_MultipleIntervalsPassed()
    {
        var timestamp = DateTime.UtcNow.AddHours(-5);
        var measurement = new MeasurementLevel
        {
            DevEui = "devEx1",
            Timestamp = timestamp,
            BatV = 3.7,
            RssiDbm = -80,
            DistanceMm = 1000
        };
        // 7200s = 2 hour interval
        var accountSensor = CreateAccountSensor(expectedIntervalSecs: 7200);

        var ex = new MeasurementLevelEx(measurement, accountSensor);
        var nextRefresh = ex.EstimateNextRefresh();

        // 5 hours ago → 18000 / 7200 = 2 → (2+1)*7200 = 21600 → timestamp + 21600 + 5
        // That's 6 hours after timestamp = 1 hour from now
        Assert.True(nextRefresh > DateTime.UtcNow);
    }

    [Fact]
    public void GetValues_ReturnsExpectedDictionary()
    {
        var measurement = new MeasurementLevel
        {
            DevEui = "devEx1",
            Timestamp = DateTime.UtcNow,
            BatV = 3.7,
            RssiDbm = -80,
            DistanceMm = 1500
        };
        var accountSensor = CreateAccountSensor();

        var ex = new MeasurementLevelEx(measurement, accountSensor);
        var values = ex.GetValues();

        Assert.Single(values);
        Assert.Equal(1500, values["DistanceMm"]);
    }
}

using Core.Entities;
using Site.Utilities;
using Xunit;

namespace SiteTests.Utilities;

public class AggregatedMeasurementLevelExTest
{
    private static AccountSensor CreateAccountSensor(
        int? distanceMmEmpty = 2000,
        int? distanceMmFull = 500,
        int? capacityL = 5000)
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
            CreateTimestamp = DateTime.UtcNow
        };
        return new AccountSensor
        {
            Account = account,
            Sensor = sensor,
            CreateTimestamp = DateTime.UtcNow,
            DistanceMmEmpty = distanceMmEmpty,
            DistanceMmFull = distanceMmFull,
            CapacityL = capacityL
        };
    }

    [Fact]
    public void Properties_ReturnDelegatedValues()
    {
        var ts = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var aggregated = new AggregatedMeasurementLevel
        {
            DevEui = "A81758FFFE000001",
            Timestamp = ts,
            BatV = 3.6,
            RssiDbm = -80,
            MinDistanceMm = 1200,
            MeanDistanceMm = 1400,
            MaxDistanceMm = 1800,
            LastDistanceMm = 1500
        };
        var accountSensor = CreateAccountSensor();
        var ex = new AggregatedMeasurementLevelEx(aggregated, accountSensor);

        Assert.Equal("A81758FFFE000001", ex.DevEui);
        Assert.Equal(ts, ex.Timestamp);
        Assert.Equal(3.6, ex.BatV);
        Assert.Equal(-80, ex.RssiDbm);
    }

    [Fact]
    public void MinDistance_ReturnsMeasurementDistance()
    {
        var aggregated = new AggregatedMeasurementLevel
        {
            DevEui = "DEV001",
            Timestamp = DateTime.UtcNow,
            MinDistanceMm = 1200,
            MeanDistanceMm = 1400,
            MaxDistanceMm = 1800,
            LastDistanceMm = 1500
        };
        var accountSensor = CreateAccountSensor();
        var ex = new AggregatedMeasurementLevelEx(aggregated, accountSensor);

        Assert.Equal(1200, ex.MinDistance.DistanceMm);
        Assert.Equal(1400, ex.MeanDistance.DistanceMm);
        Assert.Equal(1800, ex.MaxDistance.DistanceMm);
        Assert.Equal(1500, ex.LastDistance.DistanceMm);
    }

    [Fact]
    public void NullDistances_ReturnNullDistanceMm()
    {
        var aggregated = new AggregatedMeasurementLevel
        {
            DevEui = "DEV001",
            Timestamp = DateTime.UtcNow,
            MinDistanceMm = null,
            MeanDistanceMm = null,
            MaxDistanceMm = null,
            LastDistanceMm = null
        };
        var accountSensor = CreateAccountSensor();
        var ex = new AggregatedMeasurementLevelEx(aggregated, accountSensor);

        Assert.Null(ex.MinDistance.DistanceMm);
        Assert.Null(ex.MeanDistance.DistanceMm);
        Assert.Null(ex.MaxDistance.DistanceMm);
        Assert.Null(ex.LastDistance.DistanceMm);
    }

    [Fact]
    public void RssiPrc_CalculatesCorrectly()
    {
        var aggregated = new AggregatedMeasurementLevel
        {
            DevEui = "DEV001",
            Timestamp = DateTime.UtcNow,
            RssiDbm = -90  // (-90 + 150) / 60 * 80 = 80
        };
        var accountSensor = CreateAccountSensor();
        var ex = new AggregatedMeasurementLevelEx(aggregated, accountSensor);

        Assert.Equal(80.0, ex.RssiPrc, 1);
    }

    [Fact]
    public void BatteryPrc_CalculatesCorrectly()
    {
        var aggregated = new AggregatedMeasurementLevel
        {
            DevEui = "DEV001",
            Timestamp = DateTime.UtcNow,
            BatV = 3.335  // (3.335 - 3.0) / 0.335 * 100 = 100%
        };
        var accountSensor = CreateAccountSensor();
        var ex = new AggregatedMeasurementLevelEx(aggregated, accountSensor);

        Assert.Equal(100.0, ex.BatteryPrc, 1);
    }

    [Fact]
    public void BatteryPrc_AtMinVoltage_ReturnsZero()
    {
        var aggregated = new AggregatedMeasurementLevel
        {
            DevEui = "DEV001",
            Timestamp = DateTime.UtcNow,
            BatV = 3.0  // (3.0 - 3.0) / 0.335 * 100 = 0%
        };
        var accountSensor = CreateAccountSensor();
        var ex = new AggregatedMeasurementLevelEx(aggregated, accountSensor);

        Assert.Equal(0.0, ex.BatteryPrc, 1);
    }
}

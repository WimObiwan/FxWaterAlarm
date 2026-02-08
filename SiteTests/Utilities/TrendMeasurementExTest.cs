using Core.Entities;
using Core.Util;
using Site.Utilities;
using Xunit;

namespace SiteTests.Utilities;

public class TrendMeasurementExTest
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

    private static MeasurementLevelEx CreateMeasurementEx(int distanceMm, AccountSensor accountSensor)
    {
        var measurement = new MeasurementLevel
        {
            DevEui = accountSensor.Sensor.DevEui,
            Timestamp = DateTime.UtcNow,
            DistanceMm = distanceMm,
            BatV = 3.6,
            RssiDbm = -80
        };
        return new MeasurementLevelEx(measurement, accountSensor);
    }

    [Fact]
    public void DifferenceWaterL_BothHaveValues_ReturnsCorrectDifference()
    {
        var accountSensor = CreateAccountSensor();
        // distanceMmEmpty=2000, distanceMmFull=500, capacityL=5000
        // resolutionL = 1.0 / (2000-500) * 5000 = 3.333 L/mm
        // usableCapacityL = 5000 (no unusableHeightMm)
        
        // Current: distance=1000mm, height = 2000-1000 = 1000mm
        // levelFraction = (2000-1000) / (2000-500) = 1000/1500 = 0.6667
        // waterL = 0.6667 * 5000 = 3333.3
        var current = CreateMeasurementEx(1000, accountSensor);
        
        // Trend: distance=1500mm, height = 2000-1500 = 500mm
        // levelFraction = (2000-1500) / (2000-500) = 500/1500 = 0.3333
        // waterL = 0.3333 * 5000 = 1666.7
        var trend = CreateMeasurementEx(1500, accountSensor);

        var timeSpan = TimeSpan.FromDays(7);
        var trendEx = new TrendMeasurementEx(timeSpan, trend, current);

        // Difference = current - trend = 3333.3 - 1666.7 = 1666.7
        Assert.NotNull(trendEx.DifferenceWaterL);
        Assert.Equal(1666.7, trendEx.DifferenceWaterL!.Value, 0.1);
    }

    [Fact]
    public void DifferenceWaterLPerDay_ReturnsScaledValue()
    {
        var accountSensor = CreateAccountSensor();
        var current = CreateMeasurementEx(1000, accountSensor);
        var trend = CreateMeasurementEx(1500, accountSensor);

        var timeSpan = TimeSpan.FromDays(7);
        var trendEx = new TrendMeasurementEx(timeSpan, trend, current);

        Assert.NotNull(trendEx.DifferenceWaterLPerDay);
        // DifferenceWaterL / 7 days
        Assert.Equal(trendEx.DifferenceWaterL!.Value / 7.0, trendEx.DifferenceWaterLPerDay!.Value, 1);
    }

    [Fact]
    public void DifferenceLevelFraction_BothHaveValues_ReturnsCorrectDifference()
    {
        var accountSensor = CreateAccountSensor();
        // Current: distance=1000, levelFraction = (2000-1000)/(2000-500) = 0.6667
        var current = CreateMeasurementEx(1000, accountSensor);
        // Trend: distance=1500, levelFraction = (2000-1500)/(2000-500) = 0.3333
        var trend = CreateMeasurementEx(1500, accountSensor);

        var timeSpan = TimeSpan.FromDays(7);
        var trendEx = new TrendMeasurementEx(timeSpan, trend, current);

        Assert.NotNull(trendEx.DifferenceLevelFraction);
        // 0.6667 - 0.3333 = 0.3333
        Assert.Equal(0.333, trendEx.DifferenceLevelFraction!.Value, 2);
    }

    [Fact]
    public void DifferenceLevelFractionPerDay_ReturnsScaledValue()
    {
        var accountSensor = CreateAccountSensor();
        var current = CreateMeasurementEx(1000, accountSensor);
        var trend = CreateMeasurementEx(1500, accountSensor);

        var timeSpan = TimeSpan.FromDays(7);
        var trendEx = new TrendMeasurementEx(timeSpan, trend, current);

        Assert.NotNull(trendEx.DifferenceLevelFractionPerDay);
        Assert.Equal(trendEx.DifferenceLevelFraction!.Value / 7.0, trendEx.DifferenceLevelFractionPerDay!.Value, 4);
    }

    [Fact]
    public void DifferenceHeight_BothHaveValues_ReturnsCorrectDifference()
    {
        var accountSensor = CreateAccountSensor();
        // Current: distance=1000, height = 2000-1000 = 1000
        var current = CreateMeasurementEx(1000, accountSensor);
        // Trend: distance=1500, height = 2000-1500 = 500
        var trend = CreateMeasurementEx(1500, accountSensor);

        var timeSpan = TimeSpan.FromDays(7);
        var trendEx = new TrendMeasurementEx(timeSpan, trend, current);

        // 1000 - 500 = 500
        Assert.Equal(500.0, trendEx.DifferenceHeight!.Value, 1);
    }

    [Fact]
    public void DifferenceHeightPerDay_ReturnsScaledValue()
    {
        var accountSensor = CreateAccountSensor();
        var current = CreateMeasurementEx(1000, accountSensor);
        var trend = CreateMeasurementEx(1500, accountSensor);

        var timeSpan = TimeSpan.FromDays(7);
        var trendEx = new TrendMeasurementEx(timeSpan, trend, current);

        Assert.Equal(trendEx.DifferenceHeight!.Value / 7.0, trendEx.DifferenceHeightPerDay!.Value, 1);
    }

    [Fact]
    public void TimeTillEmpty_WaterDecreasing_ReturnsTimeSpan()
    {
        var accountSensor = CreateAccountSensor();
        // Current has LESS water than trend => water is decreasing
        // Current: distance=1500 => lower water level
        // Trend: distance=1000 => higher water level (past had more water)
        var current = CreateMeasurementEx(1500, accountSensor);
        var trend = CreateMeasurementEx(1000, accountSensor);

        var timeSpan = TimeSpan.FromDays(7);
        var trendEx = new TrendMeasurementEx(timeSpan, trend, current);

        Assert.NotNull(trendEx.TimeTillEmpty);
        Assert.True(trendEx.TimeTillEmpty!.Value > TimeSpan.Zero);
    }

    [Fact]
    public void TimeTillEmpty_WaterIncreasing_ReturnsNull()
    {
        var accountSensor = CreateAccountSensor();
        // Current has MORE water than trend => water is increasing
        var current = CreateMeasurementEx(1000, accountSensor);
        var trend = CreateMeasurementEx(1500, accountSensor);

        var timeSpan = TimeSpan.FromDays(7);
        var trendEx = new TrendMeasurementEx(timeSpan, trend, current);

        Assert.Null(trendEx.TimeTillEmpty);
    }

    [Fact]
    public void TimeTillFull_WaterIncreasing_ReturnsTimeSpan()
    {
        var accountSensor = CreateAccountSensor();
        // Current has MORE water than trend => water is increasing
        var current = CreateMeasurementEx(1000, accountSensor);
        var trend = CreateMeasurementEx(1500, accountSensor);

        var timeSpan = TimeSpan.FromDays(7);
        var trendEx = new TrendMeasurementEx(timeSpan, trend, current);

        Assert.NotNull(trendEx.TimeTillFull);
        Assert.True(trendEx.TimeTillFull!.Value > TimeSpan.Zero);
    }

    [Fact]
    public void TimeTillFull_WaterDecreasing_ReturnsNull()
    {
        var accountSensor = CreateAccountSensor();
        // Water is decreasing
        var current = CreateMeasurementEx(1500, accountSensor);
        var trend = CreateMeasurementEx(1000, accountSensor);

        var timeSpan = TimeSpan.FromDays(7);
        var trendEx = new TrendMeasurementEx(timeSpan, trend, current);

        Assert.Null(trendEx.TimeTillFull);
    }

    [Fact]
    public void AllDifferences_NullWhenNoCalibration()
    {
        // AccountSensor without calibration data => WaterL/LevelFraction/Height are all null
        var accountSensor = CreateAccountSensor(distanceMmEmpty: null, distanceMmFull: null, capacityL: null);
        var current = CreateMeasurementEx(1000, accountSensor);
        var trend = CreateMeasurementEx(1500, accountSensor);

        var timeSpan = TimeSpan.FromDays(7);
        var trendEx = new TrendMeasurementEx(timeSpan, trend, current);

        Assert.Null(trendEx.DifferenceWaterL);
        Assert.Null(trendEx.DifferenceWaterLPerDay);
        Assert.Null(trendEx.DifferenceLevelFraction);
        Assert.Null(trendEx.DifferenceLevelFractionPerDay);
        Assert.Null(trendEx.DifferenceHeight);
        Assert.Null(trendEx.DifferenceHeightPerDay);
        Assert.Null(trendEx.TimeTillEmpty);
        Assert.Null(trendEx.TimeTillFull);
    }
}

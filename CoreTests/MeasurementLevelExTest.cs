using Core.Entities;
using Core.Util;
using Xunit;

namespace CoreTests;

public class MeasurementLevelExTest
{
    [Fact]
    public void TestLevel()
    {
        // Arrange
        var measurement = new MeasurementLevel
        {
            DevEui = "dev123",
            Timestamp = new DateTime(2025, 8, 9, 12, 0, 0),
            BatV = 3.7,
            RssiDbm = -80,
            DistanceMm = 1200
        };
        var sensor = new Sensor
        {
            Uid = Guid.NewGuid(),
            DevEui = "dev123",
            CreateTimestamp = DateTime.UtcNow,
            Type = SensorType.Level
        };
        var account = new Account
        {
            Uid = Guid.NewGuid(),
            Email = "test@example.com",
            CreationTimestamp = DateTime.UtcNow
        };
        var accountSensor = new AccountSensor
        {
            Sensor = sensor,
            DistanceMmEmpty = 3000,
            DistanceMmFull = 800,
            UnusableHeightMm = 200,
            CapacityL = 10000,
            CreateTimestamp = DateTime.UtcNow,
            Account = account
        };

        // Act
        var ex = new MeasurementLevelEx(measurement, accountSensor);

        // Assert
        Assert.Equal(accountSensor, ex.AccountSensor);
        Assert.Equal(measurement.Timestamp, ex.Timestamp);
        Assert.Equal(measurement.BatV, ex.BatV);
        Assert.Equal(measurement.RssiDbm, ex.RssiDbm);
        Assert.Equal("dev123", ex.DevEui);
        Assert.Equal(1200, ex.Distance.DistanceMm);
        Assert.Equal(1800, ex.Distance.HeightMm); // 3000 - 1200
        Assert.Equal((3000.0 - 1200.0 - 200.0) / (3000.0 - 800.0 - 200.0), ex.Distance.LevelFraction); // (2000 - 1500) / (2000 - 100)
        Assert.Equal((3000.0 - 1200.0 - 200.0) / (3000.0 - 800.0 - 200.0) * (10000 * (3000.0 - 800.0 - 200.0) / (3000.0 - 800.0)), ex.Distance.WaterL);
    }

    [Fact]
    public void TestLevelPressure()
    {
        // Arrange
        var measurement = new MeasurementLevel
        {
            DevEui = "dev456",
            Timestamp = new DateTime(2025, 8, 9, 13, 0, 0),
            BatV = 3.8,
            RssiDbm = -75,
            DistanceMm = 1700 // Example pressure value
        };
        var sensor = new Sensor
        {
            Uid = Guid.NewGuid(),
            DevEui = "dev456",
            CreateTimestamp = DateTime.UtcNow,
            Type = SensorType.LevelPressure
        };
        var account = new Account
        {
            Uid = Guid.NewGuid(),
            Email = "pressure@example.com",
            CreationTimestamp = DateTime.UtcNow
        };
        var accountSensor = new AccountSensor
        {
            Sensor = sensor,
            DistanceMmEmpty = 100,
            DistanceMmFull = 1900,
            UnusableHeightMm = 200,
            CapacityL = 10000,
            CreateTimestamp = DateTime.UtcNow,
            Account = account
        };

        // Act
        var ex = new MeasurementLevelEx(measurement, accountSensor);

        // Assert
        Assert.Equal(accountSensor, ex.AccountSensor);
        Assert.Equal(measurement.Timestamp, ex.Timestamp);
        Assert.Equal(measurement.BatV, ex.BatV);
        Assert.Equal(measurement.RssiDbm, ex.RssiDbm);
        Assert.Equal("dev456", ex.DevEui);
        Assert.Equal(1700, ex.Distance.DistanceMm);
        Assert.Equal(1800, ex.Distance.HeightMm); // 1700 + 100
        Assert.Equal((1700.0 + 100.0 - 200.0) / (1900.0 + 100.0 - 200.0), ex.Distance.LevelFraction); // (2000 - 1500) / (2000 - 100)
        Assert.Equal((1700.0 + 100.0 - 200.0) / (1900.0 + 100.0 - 200.0) * (10000 * (1900.0 + 100.0 - 200.0) / (1900.0 + 100.0)), ex.Distance.WaterL);
    }

    [Fact]
    public void TestLevelWithManholeCompensation()
    {
        // Arrange
        // Well: DistanceMmEmpty=3000, DistanceMmFull=800, UnusableHeight=200
        // Usable height = 2000 mm (from 800 to 2800)
        // Capacity = 10000 L
        // Resolution = 10000 / 2200 = 4.545 L/mm (cross-sectional area ~ 4.545 m²)
        // Measurement at DistanceMm=300 means height = 2700 mm
        // This is 2500 mm usable height (100 mm overflow above 100%)
        var measurement = new MeasurementLevel
        {
            DevEui = "dev789",
            Timestamp = new DateTime(2025, 8, 9, 14, 0, 0),
            BatV = 3.6,
            RssiDbm = -70,
            DistanceMm = 300 // 2700 mm height, 500 mm overflow above full (2200 mm)
        };
        var sensor = new Sensor
        {
            Uid = Guid.NewGuid(),
            DevEui = "dev789",
            CreateTimestamp = DateTime.UtcNow,
            Type = SensorType.Level
        };
        var account = new Account
        {
            Uid = Guid.NewGuid(),
            Email = "manhole@example.com",
            CreationTimestamp = DateTime.UtcNow
        };
        var accountSensor = new AccountSensor
        {
            Sensor = sensor,
            DistanceMmEmpty = 3000,
            DistanceMmFull = 800,
            UnusableHeightMm = 200,
            CapacityL = 10000,
            CreateTimestamp = DateTime.UtcNow,
            Account = account,
            NoMinMaxConstraints = true,  // No longer used but kept for backward compatibility
            ManholeAreaM2 = 1.0  // 1 m² manhole area
        };

        // Act
        var ex = new MeasurementLevelEx(measurement, accountSensor);

        // Assert basic properties
        Assert.Equal(300, ex.Distance.DistanceMm);
        Assert.Equal(2700, ex.Distance.HeightMm);
        
        // Calculate expected values
        // Usable height = 2200 mm (3000 - 800)
        // Height from bottom = 2700 mm (3000 - 300)
        // Usable height from bottom = 2500 mm (2700 - 200 unusable)
        // RealLevelFraction = 2500 / 2000 = 1.25 (125% of usable capacity)
        
        // With manhole compensation:
        // Usable capacity = 10000 * (2000 / 2200) = 9090.909 L
        // Overflow fraction = 0.25 (25% over)
        // Overflow height = 0.25 * 2000 = 500 mm
        // Manhole volume = 500 * 1.0 = 500 L (manhole has 1 m² area = 1 L/mm)
        // Total volume = 9090.909 + 500 = 9590.909 L
        // Equivalent fraction = 9590.909 / 9090.909 = 1.055
        
        var expectedRealLevelFraction = 1.25;
        var usableCapacityL = 10000.0 * (2000.0 / 2200.0);
        var overflowFraction = 0.25;
        var overflowHeightMm = overflowFraction * 2000.0;
        var manholeVolumeL = overflowHeightMm * 1.0;
        var expectedTotalVolumeL = usableCapacityL + manholeVolumeL;
        var expectedLevelFraction = expectedTotalVolumeL / usableCapacityL;
        
        Assert.Equal(expectedRealLevelFraction, ex.Distance.RealLevelFraction!.Value, 3);
        Assert.Equal(expectedLevelFraction, ex.Distance.LevelFraction!.Value, 3);
        Assert.Equal(expectedTotalVolumeL, ex.Distance.WaterL!.Value, 2);
    }

    [Fact]
    public void TestLevelPressureWithManholeCompensation()
    {
        // Arrange
        // Well: DistanceMmEmpty=100, DistanceMmFull=1900, UnusableHeight=200
        // Usable height = 1800 mm
        // Capacity = 9000 L
        // Resolution = 9000 / 2000 = 4.5 L/mm
        // Measurement at DistanceMm=2200 means height = 2300 mm (overflow of 300 mm above full)
        var measurement = new MeasurementLevel
        {
            DevEui = "dev101",
            Timestamp = new DateTime(2025, 8, 9, 15, 0, 0),
            BatV = 3.9,
            RssiDbm = -65,
            DistanceMm = 2200
        };
        var sensor = new Sensor
        {
            Uid = Guid.NewGuid(),
            DevEui = "dev101",
            CreateTimestamp = DateTime.UtcNow,
            Type = SensorType.LevelPressure
        };
        var account = new Account
        {
            Uid = Guid.NewGuid(),
            Email = "manhole-pressure@example.com",
            CreationTimestamp = DateTime.UtcNow
        };
        var accountSensor = new AccountSensor
        {
            Sensor = sensor,
            DistanceMmEmpty = 100,
            DistanceMmFull = 1900,
            UnusableHeightMm = 200,
            CapacityL = 9000,
            CreateTimestamp = DateTime.UtcNow,
            Account = account,
            NoMinMaxConstraints = true,  // No longer used but kept for backward compatibility
            ManholeAreaM2 = 1.0  // 1 m² manhole area
        };

        // Act
        var ex = new MeasurementLevelEx(measurement, accountSensor);

        // Assert
        Assert.Equal(2200, ex.Distance.DistanceMm);
        Assert.Equal(2300, ex.Distance.HeightMm); // 2200 + 100
        
        // RealLevelFraction = (2300 - 200) / (2000 - 200) = 2100 / 1800 = 1.1667
        var expectedRealLevelFraction = 2100.0 / 1800.0;
        var usableCapacityL = 9000.0 * (1800.0 / 2000.0);
        var overflowFraction = expectedRealLevelFraction - 1.0;
        var overflowHeightMm = overflowFraction * 1800.0;
        var manholeVolumeL = overflowHeightMm * 1.0;
        var expectedTotalVolumeL = usableCapacityL + manholeVolumeL;
        var expectedLevelFraction = expectedTotalVolumeL / usableCapacityL;
        
        Assert.Equal(expectedRealLevelFraction, ex.Distance.RealLevelFraction!.Value, 3);
        Assert.Equal(expectedLevelFraction, ex.Distance.LevelFraction!.Value, 3);
        Assert.Equal(expectedTotalVolumeL, ex.Distance.WaterL!.Value, 2);
    }

    [Fact]
    public void TestLevelWithOverflow_AlwaysAppliesManholeCompensation()
    {
        // Test that overflow always applies manhole compensation (NoMinMaxConstraints always true behavior)
        var measurement = new MeasurementLevel
        {
            DevEui = "dev999",
            Timestamp = new DateTime(2025, 8, 9, 16, 0, 0),
            BatV = 3.5,
            RssiDbm = -85,
            DistanceMm = 300 // 125% if not compensated
        };
        var sensor = new Sensor
        {
            Uid = Guid.NewGuid(),
            DevEui = "dev999",
            CreateTimestamp = DateTime.UtcNow,
            Type = SensorType.Level
        };
        var account = new Account
        {
            Uid = Guid.NewGuid(),
            Email = "overflow@example.com",
            CreationTimestamp = DateTime.UtcNow
        };
        var accountSensor = new AccountSensor
        {
            Sensor = sensor,
            DistanceMmEmpty = 3000,
            DistanceMmFull = 800,
            UnusableHeightMm = 200,
            CapacityL = 10000,
            CreateTimestamp = DateTime.UtcNow,
            Account = account,
            NoMinMaxConstraints = false,  // No longer used
            ManholeAreaM2 = 1.0  // 1 m² manhole area - compensation applied when this is set
        };

        // Act
        var ex = new MeasurementLevelEx(measurement, accountSensor);

        // Assert - should apply manhole compensation even though NoMinMaxConstraints is false
        // RealLevelFraction = 1.25 (125%)
        var expectedRealLevelFraction = 1.25;
        var usableCapacityL = 10000.0 * (2000.0 / 2200.0);
        var overflowFraction = 0.25;
        var overflowHeightMm = overflowFraction * 2000.0;
        var manholeVolumeL = overflowHeightMm * 1.0;
        var expectedTotalVolumeL = usableCapacityL + manholeVolumeL;
        var expectedLevelFraction = expectedTotalVolumeL / usableCapacityL;
        
        Assert.Equal(expectedRealLevelFraction, ex.Distance.RealLevelFraction!.Value, 3);
        Assert.Equal(expectedLevelFraction, ex.Distance.LevelFraction!.Value, 3);
        Assert.Equal(expectedTotalVolumeL, ex.Distance.WaterL!.Value, 2);
    }

    [Fact]
    public void TestLevelWithOverflow_NoManholeAreaSet()
    {
        // Test that when ManholeAreaM2 is null, no compensation is applied (overflow allowed but no manhole)
        var measurement = new MeasurementLevel
        {
            DevEui = "dev888",
            Timestamp = new DateTime(2025, 8, 9, 17, 0, 0),
            BatV = 3.6,
            RssiDbm = -75,
            DistanceMm = 300 // 125% if not compensated
        };
        var sensor = new Sensor
        {
            Uid = Guid.NewGuid(),
            DevEui = "dev888",
            CreateTimestamp = DateTime.UtcNow,
            Type = SensorType.Level
        };
        var account = new Account
        {
            Uid = Guid.NewGuid(),
            Email = "no-manhole@example.com",
            CreationTimestamp = DateTime.UtcNow
        };
        var accountSensor = new AccountSensor
        {
            Sensor = sensor,
            DistanceMmEmpty = 3000,
            DistanceMmFull = 800,
            UnusableHeightMm = 200,
            CapacityL = 10000,
            CreateTimestamp = DateTime.UtcNow,
            Account = account,
            ManholeAreaM2 = null  // No manhole area set - no compensation
        };

        // Act
        var ex = new MeasurementLevelEx(measurement, accountSensor);

        // Assert - should NOT apply manhole compensation when ManholeAreaM2 is null
        // RealLevelFraction = 1.25 (125%)
        var expectedRealLevelFraction = 1.25;
        var usableCapacityL = 10000.0 * (2000.0 / 2200.0);
        
        // Without manhole compensation, RealLevelFraction should equal RealLevelFraction
        Assert.Equal(expectedRealLevelFraction, ex.Distance.RealLevelFraction!.Value, 3);
        // Without manhole compensation, LevelFraction should be capped at 1.0 (100%)
        Assert.Equal(1.0, ex.Distance.LevelFraction!.Value, 3);
        
        // Water volume is just the fraction times capacity (no manhole adjustment)
        var expectedWaterL = usableCapacityL;
        Assert.Equal(expectedWaterL, ex.Distance.WaterL!.Value, 2);
    }
}

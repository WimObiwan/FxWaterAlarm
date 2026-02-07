using Core.Entities;
using Core.Util;
using Xunit;

namespace CoreTests;

public class MeasurementDistanceTest
{
    private static AccountSensor CreateLevelSensor(
        int? distanceMmEmpty = 3000,
        int? distanceMmFull = 800,
        int? unusableHeightMm = 200,
        int? capacityL = 10000,
        double? manholeAreaM2 = null)
    {
        var sensor = new Sensor
        {
            Uid = Guid.NewGuid(),
            DevEui = "devDist1",
            CreateTimestamp = DateTime.UtcNow,
            Type = SensorType.Level
        };
        var account = new Account
        {
            Uid = Guid.NewGuid(),
            Email = "dist@example.com",
            CreationTimestamp = DateTime.UtcNow
        };
        return new AccountSensor
        {
            Sensor = sensor,
            Account = account,
            CreateTimestamp = DateTime.UtcNow,
            DistanceMmEmpty = distanceMmEmpty,
            DistanceMmFull = distanceMmFull,
            UnusableHeightMm = unusableHeightMm,
            CapacityL = capacityL,
            ManholeAreaM2 = manholeAreaM2
        };
    }

    private static AccountSensor CreateLevelPressureSensor(
        int? distanceMmEmpty = 100,
        int? distanceMmFull = 1900,
        int? unusableHeightMm = 200,
        int? capacityL = 10000,
        double? manholeAreaM2 = null)
    {
        var sensor = new Sensor
        {
            Uid = Guid.NewGuid(),
            DevEui = "devDist2",
            CreateTimestamp = DateTime.UtcNow,
            Type = SensorType.LevelPressure
        };
        var account = new Account
        {
            Uid = Guid.NewGuid(),
            Email = "dist-pressure@example.com",
            CreationTimestamp = DateTime.UtcNow
        };
        return new AccountSensor
        {
            Sensor = sensor,
            Account = account,
            CreateTimestamp = DateTime.UtcNow,
            DistanceMmEmpty = distanceMmEmpty,
            DistanceMmFull = distanceMmFull,
            UnusableHeightMm = unusableHeightMm,
            CapacityL = capacityL,
            ManholeAreaM2 = manholeAreaM2
        };
    }

    // --- DistanceMm null tests ---

    [Fact]
    public void DistanceMm_Null_ReturnsNull()
    {
        var accountSensor = CreateLevelSensor();
        var distance = new MeasurementDistance(null, accountSensor);

        Assert.Null(distance.DistanceMm);
        Assert.Null(distance.HeightMm);
        Assert.Null(distance.RealLevelFraction);
        Assert.Null(distance.LevelFraction);
        Assert.Null(distance.RealLevelFractionIncludingUnusableHeight);
        Assert.Null(distance.LevelFractionIncludingUnusableHeight);
        Assert.Null(distance.WaterL);
    }

    // --- HeightMm tests ---

    [Fact]
    public void HeightMm_Level_WithEmptyDistance()
    {
        var accountSensor = CreateLevelSensor(distanceMmEmpty: 3000);
        var distance = new MeasurementDistance(1200, accountSensor);

        Assert.Equal(1800, distance.HeightMm); // 3000 - 1200
    }

    [Fact]
    public void HeightMm_Level_WithoutEmptyDistance_ReturnsNull()
    {
        var accountSensor = CreateLevelSensor(distanceMmEmpty: null);
        var distance = new MeasurementDistance(1200, accountSensor);

        Assert.Null(distance.HeightMm);
    }

    [Fact]
    public void HeightMm_LevelPressure_WithEmptyDistance()
    {
        var accountSensor = CreateLevelPressureSensor(distanceMmEmpty: 100);
        var distance = new MeasurementDistance(1700, accountSensor);

        Assert.Equal(1800, distance.HeightMm); // 1700 + 100
    }

    [Fact]
    public void HeightMm_LevelPressure_WithNullEmptyDistance()
    {
        var accountSensor = CreateLevelPressureSensor(distanceMmEmpty: null);
        var distance = new MeasurementDistance(1700, accountSensor);

        Assert.Equal(1700, distance.HeightMm); // 1700 + 0
    }

    // --- RealLevelFraction tests ---

    [Fact]
    public void RealLevelFraction_Level_Normal()
    {
        // Empty=3000, Full=800, Unusable=200, Distance=1200
        // (3000 - 1200 - 200) / (3000 - 800 - 200) = 1600 / 2000 = 0.8
        var accountSensor = CreateLevelSensor();
        var distance = new MeasurementDistance(1200, accountSensor);

        Assert.NotNull(distance.RealLevelFraction);
        Assert.Equal(0.8, distance.RealLevelFraction!.Value, 3);
    }

    [Fact]
    public void RealLevelFraction_Level_MissingEmptyDistance_ReturnsNull()
    {
        var accountSensor = CreateLevelSensor(distanceMmEmpty: null);
        var distance = new MeasurementDistance(1200, accountSensor);

        Assert.Null(distance.RealLevelFraction);
    }

    [Fact]
    public void RealLevelFraction_Level_MissingFullDistance_ReturnsNull()
    {
        var accountSensor = CreateLevelSensor(distanceMmFull: null);
        var distance = new MeasurementDistance(1200, accountSensor);

        Assert.Null(distance.RealLevelFraction);
    }

    [Fact]
    public void RealLevelFraction_LevelPressure_Normal()
    {
        // Empty=100, Full=1900, Unusable=200, Distance=1700
        // (100 + 1700 - 200) / (100 + 1900 - 200) = 1600 / 1800 ≈ 0.889
        var accountSensor = CreateLevelPressureSensor();
        var distance = new MeasurementDistance(1700, accountSensor);

        Assert.NotNull(distance.RealLevelFraction);
        Assert.Equal(1600.0 / 1800.0, distance.RealLevelFraction!.Value, 3);
    }

    [Fact]
    public void RealLevelFraction_LevelPressure_MissingFull_ReturnsNull()
    {
        var accountSensor = CreateLevelPressureSensor(distanceMmFull: null);
        var distance = new MeasurementDistance(1700, accountSensor);

        Assert.Null(distance.RealLevelFraction);
    }

    [Fact]
    public void RealLevelFraction_LevelPressure_NullEmpty_UsesZero()
    {
        // Empty=null → uses 0, Full=1900, Unusable=200, Distance=1700
        // (0 + 1700 - 200) / (0 + 1900 - 200) = 1500 / 1700
        var accountSensor = CreateLevelPressureSensor(distanceMmEmpty: null);
        var distance = new MeasurementDistance(1700, accountSensor);

        Assert.NotNull(distance.RealLevelFraction);
        Assert.Equal(1500.0 / 1700.0, distance.RealLevelFraction!.Value, 3);
    }

    // --- LevelFraction clamping tests ---

    [Fact]
    public void LevelFraction_Level_ClampsToZero_WhenNegative()
    {
        // Distance > Empty → negative level fraction
        // Empty=3000, Full=800, Unusable=200, Distance=3500
        // (3000 - 3500 - 200) / (3000 - 800 - 200) = -700 / 2000 = -0.35
        var accountSensor = CreateLevelSensor();
        var distance = new MeasurementDistance(3500, accountSensor);

        Assert.NotNull(distance.LevelFraction);
        Assert.Equal(0.0, distance.LevelFraction!.Value);
    }

    [Fact]
    public void LevelFraction_Normal_NoClampingNeeded()
    {
        // A normal case where fraction is between 0 and 1
        var accountSensor = CreateLevelSensor();
        var distance = new MeasurementDistance(1200, accountSensor);

        Assert.NotNull(distance.LevelFraction);
        Assert.Equal(0.8, distance.LevelFraction!.Value, 3);
    }

    [Fact]
    public void LevelFraction_NullDistance_ReturnsNull()
    {
        var accountSensor = CreateLevelSensor();
        var distance = new MeasurementDistance(null, accountSensor);

        Assert.Null(distance.LevelFraction);
    }

    // --- DRealLevelFractionIncludingUnusableHeight ---

    [Fact]
    public void RealLevelFractionIncludingUnusableHeight_Level_Normal()
    {
        // Empty=3000, Full=800, Distance=1200
        // (3000 - 1200) / (3000 - 800) = 1800 / 2200 ≈ 0.818
        var accountSensor = CreateLevelSensor();
        var distance = new MeasurementDistance(1200, accountSensor);

        Assert.NotNull(distance.RealLevelFractionIncludingUnusableHeight);
        Assert.Equal(1800.0 / 2200.0, distance.RealLevelFractionIncludingUnusableHeight!.Value, 3);
    }

    [Fact]
    public void RealLevelFractionIncludingUnusableHeight_Level_MissingEmpty_ReturnsNull()
    {
        var accountSensor = CreateLevelSensor(distanceMmEmpty: null);
        var distance = new MeasurementDistance(1200, accountSensor);

        Assert.Null(distance.RealLevelFractionIncludingUnusableHeight);
    }

    [Fact]
    public void RealLevelFractionIncludingUnusableHeight_Level_MissingFull_ReturnsNull()
    {
        var accountSensor = CreateLevelSensor(distanceMmFull: null);
        var distance = new MeasurementDistance(1200, accountSensor);

        Assert.Null(distance.RealLevelFractionIncludingUnusableHeight);
    }

    [Fact]
    public void RealLevelFractionIncludingUnusableHeight_LevelPressure_Normal()
    {
        // Empty=100, Full=1900, Distance=1700
        // (100 + 1700) / (100 + 1900) = 1800 / 2000 = 0.9
        var accountSensor = CreateLevelPressureSensor();
        var distance = new MeasurementDistance(1700, accountSensor);

        Assert.NotNull(distance.RealLevelFractionIncludingUnusableHeight);
        Assert.Equal(1800.0 / 2000.0, distance.RealLevelFractionIncludingUnusableHeight!.Value, 3);
    }

    [Fact]
    public void RealLevelFractionIncludingUnusableHeight_LevelPressure_MissingFull_ReturnsNull()
    {
        var accountSensor = CreateLevelPressureSensor(distanceMmFull: null);
        var distance = new MeasurementDistance(1700, accountSensor);

        Assert.Null(distance.RealLevelFractionIncludingUnusableHeight);
    }

    [Fact]
    public void RealLevelFractionIncludingUnusableHeight_LevelPressure_NullEmpty_UsesZero()
    {
        // Empty=null → 0, Full=1900, Distance=1700
        // (0 + 1700) / (0 + 1900) = 1700 / 1900
        var accountSensor = CreateLevelPressureSensor(distanceMmEmpty: null);
        var distance = new MeasurementDistance(1700, accountSensor);

        Assert.NotNull(distance.RealLevelFractionIncludingUnusableHeight);
        Assert.Equal(1700.0 / 1900.0, distance.RealLevelFractionIncludingUnusableHeight!.Value, 3);
    }

    // --- LevelFractionIncludingUnusableHeight tests ---

    [Fact]
    public void LevelFractionIncludingUnusableHeight_Normal_NoClamp()
    {
        var accountSensor = CreateLevelSensor();
        var distance = new MeasurementDistance(1200, accountSensor);

        Assert.NotNull(distance.LevelFractionIncludingUnusableHeight);
        Assert.Equal(1800.0 / 2200.0, distance.LevelFractionIncludingUnusableHeight!.Value, 3);
    }

    [Fact]
    public void LevelFractionIncludingUnusableHeight_ClampsToZero_WhenNegative()
    {
        // Distance > Empty → negative
        var accountSensor = CreateLevelSensor();
        var distance = new MeasurementDistance(3500, accountSensor);

        Assert.NotNull(distance.LevelFractionIncludingUnusableHeight);
        Assert.Equal(0.0, distance.LevelFractionIncludingUnusableHeight!.Value);
    }

    [Fact]
    public void LevelFractionIncludingUnusableHeight_Null_WhenDistanceNull()
    {
        var accountSensor = CreateLevelSensor();
        var distance = new MeasurementDistance(null, accountSensor);

        Assert.Null(distance.LevelFractionIncludingUnusableHeight);
    }

    // --- WaterL tests ---

    [Fact]
    public void WaterL_CalculatesCorrectly()
    {
        // LevelFraction * UsableCapacityL
        var accountSensor = CreateLevelSensor(capacityL: 10000);
        var distance = new MeasurementDistance(1200, accountSensor);

        Assert.NotNull(distance.WaterL);
        var expectedFraction = 0.8; // (3000-1200-200)/(3000-800-200)
        var usableCapacityL = accountSensor.UsableCapacityL!.Value;
        Assert.Equal(expectedFraction * usableCapacityL, distance.WaterL!.Value, 1);
    }

    [Fact]
    public void WaterL_NullCapacity_ReturnsNull()
    {
        var accountSensor = CreateLevelSensor(capacityL: null);
        var distance = new MeasurementDistance(1200, accountSensor);

        Assert.Null(distance.WaterL);
    }

    [Fact]
    public void WaterL_NullDistance_ReturnsNull()
    {
        var accountSensor = CreateLevelSensor();
        var distance = new MeasurementDistance(null, accountSensor);

        Assert.Null(distance.WaterL);
    }

    // --- Manhole compensation on LevelFractionIncludingUnusableHeight ---

    [Fact]
    public void LevelFractionIncludingUnusableHeight_WithManhole_AppliesCompensation()
    {
        // Overflow scenario including unusable height
        // Empty=3000, Full=800, Distance=300
        // RealLevelFractionIncUnusable = (3000-300) / (3000-800) = 2700/2200 ≈ 1.227
        // This > 1.0, so manhole compensation applies
        var accountSensor = CreateLevelSensor(manholeAreaM2: 1.0);
        var distance = new MeasurementDistance(300, accountSensor);

        Assert.NotNull(distance.LevelFractionIncludingUnusableHeight);
        Assert.True(distance.LevelFractionIncludingUnusableHeight!.Value > 1.0);
    }

    [Fact]
    public void LevelFractionIncludingUnusableHeight_WithoutManhole_CapsAt1()
    {
        // Overflow without manhole → caps at 1.0
        var accountSensor = CreateLevelSensor(manholeAreaM2: null);
        var distance = new MeasurementDistance(300, accountSensor);

        Assert.NotNull(distance.LevelFractionIncludingUnusableHeight);
        Assert.Equal(1.0, distance.LevelFractionIncludingUnusableHeight!.Value, 3);
    }

    // --- Manhole compensation edge cases ---

    [Fact]
    public void LevelFraction_ManholeAreaZero_CapsAtOne()
    {
        // ManholeAreaM2 = 0 means no manhole compensation
        var accountSensor = CreateLevelSensor(manholeAreaM2: 0.0);
        var distance = new MeasurementDistance(300, accountSensor);

        Assert.Equal(1.0, distance.LevelFraction!.Value, 3);
    }

    [Fact]
    public void LevelFraction_NoCapacityOrResolution_ReturnsRawFraction()
    {
        // When capacityL or resolutionL is null, manhole formula can't compute
        // but the raw realLevelFraction is still returned
        var accountSensor = CreateLevelSensor(capacityL: null, manholeAreaM2: 1.0);
        var distance = new MeasurementDistance(300, accountSensor);

        // Without capacity, UsableCapacityL is null, so ApplyManholeCompensation
        // returns the raw fraction since capacityL is null → no resolution
        var realFraction = distance.RealLevelFraction;
        Assert.NotNull(realFraction);
        Assert.True(realFraction!.Value > 1.0);
        // LevelFraction should just return the raw realFraction since compensation can't be applied
        Assert.Equal(realFraction.Value, distance.LevelFraction!.Value, 3);
    }

    // --- SensorType not Level or LevelPressure ---

    [Fact]
    public void HeightMm_UnsupportedSensorType_ReturnsNull()
    {
        var sensor = new Sensor
        {
            Uid = Guid.NewGuid(),
            DevEui = "devMoisture",
            CreateTimestamp = DateTime.UtcNow,
            Type = SensorType.Moisture
        };
        var account = new Account
        {
            Uid = Guid.NewGuid(),
            Email = "other@example.com",
            CreationTimestamp = DateTime.UtcNow
        };
        var accountSensor = new AccountSensor
        {
            Sensor = sensor,
            Account = account,
            CreateTimestamp = DateTime.UtcNow,
            DistanceMmEmpty = 3000,
            DistanceMmFull = 800
        };
        var distance = new MeasurementDistance(1200, accountSensor);

        Assert.Null(distance.HeightMm);
        Assert.Null(distance.RealLevelFraction);
        Assert.Null(distance.LevelFraction);
    }

    // --- LevelPressure with zero empty distance ---

    [Fact]
    public void HeightMm_LevelPressure_ZeroEmpty()
    {
        var accountSensor = CreateLevelPressureSensor(distanceMmEmpty: 0);
        var distance = new MeasurementDistance(1700, accountSensor);

        Assert.Equal(1700, distance.HeightMm); // 1700 + 0
    }

    // --- Null unusable height ---

    [Fact]
    public void RealLevelFraction_Level_NullUnusableHeight()
    {
        // When UnusableHeightMm is null, it defaults to 0
        // (3000 - 1200 - 0) / (3000 - 800 - 0) = 1800 / 2200
        var sensor = new Sensor
        {
            Uid = Guid.NewGuid(),
            DevEui = "devDist3",
            CreateTimestamp = DateTime.UtcNow,
            Type = SensorType.Level
        };
        var account = new Account
        {
            Uid = Guid.NewGuid(),
            Email = "null-unusable@example.com",
            CreationTimestamp = DateTime.UtcNow
        };
        var accountSensor = new AccountSensor
        {
            Sensor = sensor,
            Account = account,
            CreateTimestamp = DateTime.UtcNow,
            DistanceMmEmpty = 3000,
            DistanceMmFull = 800,
            UnusableHeightMm = null,
            CapacityL = 10000
        };
        var distance = new MeasurementDistance(1200, accountSensor);

        Assert.Equal(1800.0 / 2200.0, distance.RealLevelFraction!.Value, 3);
    }

    [Fact]
    public void RealLevelFraction_LevelPressure_NullUnusableHeight()
    {
        // (100 + 1700 - 0) / (100 + 1900 - 0) = 1800 / 2000 = 0.9
        var sensor = new Sensor
        {
            Uid = Guid.NewGuid(),
            DevEui = "devDist4",
            CreateTimestamp = DateTime.UtcNow,
            Type = SensorType.LevelPressure
        };
        var account = new Account
        {
            Uid = Guid.NewGuid(),
            Email = "null-unusable-p@example.com",
            CreationTimestamp = DateTime.UtcNow
        };
        var accountSensor = new AccountSensor
        {
            Sensor = sensor,
            Account = account,
            CreateTimestamp = DateTime.UtcNow,
            DistanceMmEmpty = 100,
            DistanceMmFull = 1900,
            UnusableHeightMm = null,
            CapacityL = 10000
        };
        var distance = new MeasurementDistance(1700, accountSensor);

        Assert.Equal(1800.0 / 2000.0, distance.RealLevelFraction!.Value, 3);
    }
}

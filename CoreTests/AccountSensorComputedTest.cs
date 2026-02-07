using Core.Entities;
using Core.Exceptions;
using System.Reflection;
using Xunit;

namespace CoreTests;

public class AccountSensorComputedTest
{
    private static Account CreateAccount(string? link = "acclink")
    {
        return new Account
        {
            Uid = Guid.NewGuid(),
            Email = "test@example.com",
            CreationTimestamp = DateTime.UtcNow,
            Link = link
        };
    }

    private static Sensor CreateSensor(SensorType type, string? link = "sensorlink")
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

    private static AccountSensor CreateAccountSensor(
        SensorType type,
        int? distanceMmEmpty = 3000,
        int? distanceMmFull = 800,
        int? capacityL = 10000,
        int? unusableHeightMm = 200,
        string? accountLink = "acclink",
        string? sensorLink = "sensorlink",
        bool disabled = false)
    {
        var accountSensor = new AccountSensor
        {
            Sensor = CreateSensor(type, sensorLink),
            Account = CreateAccount(accountLink),
            CreateTimestamp = DateTime.UtcNow,
            DistanceMmEmpty = distanceMmEmpty,
            DistanceMmFull = distanceMmFull,
            CapacityL = capacityL,
            UnusableHeightMm = unusableHeightMm,
            Disabled = disabled
        };

        // Initialize private _alarms field
        var alarmsField = typeof(AccountSensor).GetField("_alarms", BindingFlags.NonPublic | BindingFlags.Instance);
        alarmsField?.SetValue(accountSensor, new List<AccountSensorAlarm>());

        return accountSensor;
    }

    // --- ResolutionL ---

    [Fact]
    public void ResolutionL_Level_CalculatesCorrectly()
    {
        // 1.0 / (3000 - 800) * 10000 = 4.545...
        var as1 = CreateAccountSensor(SensorType.Level);
        Assert.NotNull(as1.ResolutionL);
        Assert.Equal(1.0 / (3000 - 800) * 10000, as1.ResolutionL!.Value, 3);
    }

    [Fact]
    public void ResolutionL_Level_MissingEmpty_ReturnsNull()
    {
        var as1 = CreateAccountSensor(SensorType.Level, distanceMmEmpty: null);
        Assert.Null(as1.ResolutionL);
    }

    [Fact]
    public void ResolutionL_Level_MissingFull_ReturnsNull()
    {
        var as1 = CreateAccountSensor(SensorType.Level, distanceMmFull: null);
        Assert.Null(as1.ResolutionL);
    }

    [Fact]
    public void ResolutionL_Level_MissingCapacity_ReturnsNull()
    {
        var as1 = CreateAccountSensor(SensorType.Level, capacityL: null);
        Assert.Null(as1.ResolutionL);
    }

    [Fact]
    public void ResolutionL_LevelPressure_CalculatesCorrectly()
    {
        // 1.0 / (100 + 1900) * 10000 = 5.0
        var as1 = CreateAccountSensor(SensorType.LevelPressure, distanceMmEmpty: 100, distanceMmFull: 1900);
        Assert.NotNull(as1.ResolutionL);
        Assert.Equal(1.0 / (100 + 1900) * 10000, as1.ResolutionL!.Value, 3);
    }

    [Fact]
    public void ResolutionL_LevelPressure_NullEmpty_UsesZero()
    {
        // 1.0 / (0 + 1900) * 10000
        var as1 = CreateAccountSensor(SensorType.LevelPressure, distanceMmEmpty: null, distanceMmFull: 1900);
        Assert.NotNull(as1.ResolutionL);
        Assert.Equal(1.0 / 1900 * 10000, as1.ResolutionL!.Value, 3);
    }

    [Fact]
    public void ResolutionL_LevelPressure_MissingFull_ReturnsNull()
    {
        var as1 = CreateAccountSensor(SensorType.LevelPressure, distanceMmFull: null);
        Assert.Null(as1.ResolutionL);
    }

    [Fact]
    public void ResolutionL_LevelPressure_MissingCapacity_ReturnsNull()
    {
        var as1 = CreateAccountSensor(SensorType.LevelPressure, capacityL: null);
        Assert.Null(as1.ResolutionL);
    }

    [Fact]
    public void ResolutionL_UnsupportedType_ReturnsNull()
    {
        var as1 = CreateAccountSensor(SensorType.Detect);
        Assert.Null(as1.ResolutionL);
    }

    // --- UnusableCapacityL ---

    [Fact]
    public void UnusableCapacityL_WithValues_CalculatesCorrectly()
    {
        var as1 = CreateAccountSensor(SensorType.Level, unusableHeightMm: 200);
        Assert.NotNull(as1.UnusableCapacityL);
        Assert.Equal(200 * as1.ResolutionL!.Value, as1.UnusableCapacityL!.Value, 3);
    }

    [Fact]
    public void UnusableCapacityL_NullUnusable_ReturnsNull()
    {
        var as1 = CreateAccountSensor(SensorType.Level, unusableHeightMm: null);
        Assert.Null(as1.UnusableCapacityL);
    }

    [Fact]
    public void UnusableCapacityL_NullResolution_ReturnsNull()
    {
        var as1 = CreateAccountSensor(SensorType.Detect, unusableHeightMm: 200);
        Assert.Null(as1.UnusableCapacityL);
    }

    // --- UsableCapacityL ---

    [Fact]
    public void UsableCapacityL_WithValues_CalculatesCorrectly()
    {
        var as1 = CreateAccountSensor(SensorType.Level, unusableHeightMm: 200);
        Assert.NotNull(as1.UsableCapacityL);
        var expected = 10000 - 200 * as1.ResolutionL!.Value;
        Assert.Equal(expected, as1.UsableCapacityL!.Value, 3);
    }

    [Fact]
    public void UsableCapacityL_NullCapacity_ReturnsNull()
    {
        var as1 = CreateAccountSensor(SensorType.Level, capacityL: null);
        Assert.Null(as1.UsableCapacityL);
    }

    [Fact]
    public void UsableCapacityL_NullUnusable_DefaultsToZero()
    {
        var as1 = CreateAccountSensor(SensorType.Level, unusableHeightMm: null);
        Assert.NotNull(as1.UsableCapacityL);
        // Without unusable height, UsableCapacityL = CapacityL - 0 * ResolutionL = CapacityL
        Assert.Equal(10000.0, as1.UsableCapacityL!.Value, 3);
    }

    // --- RestPath / ApiRestPath ---

    [Fact]
    public void RestPath_WithBothLinks_ReturnsPath()
    {
        var as1 = CreateAccountSensor(SensorType.Level, accountLink: "acc", sensorLink: "sen");
        Assert.Equal("/a/acc/s/sen", as1.RestPath);
    }

    [Fact]
    public void RestPath_NullAccountLink_ReturnsNull()
    {
        var as1 = CreateAccountSensor(SensorType.Level, accountLink: null, sensorLink: "sen");
        Assert.Null(as1.RestPath);
    }

    [Fact]
    public void RestPath_NullSensorLink_ReturnsNull()
    {
        var as1 = CreateAccountSensor(SensorType.Level, accountLink: "acc", sensorLink: null);
        Assert.Null(as1.RestPath);
    }

    [Fact]
    public void ApiRestPath_WithBothLinks_ReturnsPath()
    {
        var as1 = CreateAccountSensor(SensorType.Level, accountLink: "acc", sensorLink: "sen");
        Assert.Equal("/api/a/acc/s/sen", as1.ApiRestPath);
    }

    [Fact]
    public void ApiRestPath_NullAccountLink_ReturnsNull()
    {
        var as1 = CreateAccountSensor(SensorType.Level, accountLink: null);
        Assert.Null(as1.ApiRestPath);
    }

    // --- RoundVolume ---

    [Fact]
    public void RoundVolume_LessThan20_Returns100()
    {
        var as1 = CreateAccountSensor(SensorType.Level, capacityL: 10);
        Assert.Equal(100, as1.RoundVolume);
    }

    [Fact]
    public void RoundVolume_LessThan200_Returns10()
    {
        var as1 = CreateAccountSensor(SensorType.Level, capacityL: 100);
        Assert.Equal(10, as1.RoundVolume);
    }

    [Fact]
    public void RoundVolume_200OrMore_Returns1()
    {
        var as1 = CreateAccountSensor(SensorType.Level, capacityL: 10000);
        Assert.Equal(1, as1.RoundVolume);
    }

    // --- GraphType ---

    [Fact]
    public void GraphType_WithVolume_ReturnsVolume()
    {
        var as1 = CreateAccountSensor(SensorType.Level);
        Assert.Equal(GraphType.Volume, as1.GraphType);
    }

    [Fact]
    public void GraphType_WithPercentageOnly_ReturnsPercentage()
    {
        // Moisture supports percentage but not capacity/volume
        var as1 = CreateAccountSensor(SensorType.Moisture);
        Assert.Equal(GraphType.Percentage, as1.GraphType);
    }

    [Fact]
    public void GraphType_HeightOnly_ReturnsHeight()
    {
        // LevelPressure with no DistanceMmFull → no percentage, but has height
        var as1 = CreateAccountSensor(SensorType.LevelPressure, distanceMmFull: null, capacityL: null);
        Assert.Equal(GraphType.Height, as1.GraphType);
    }

    [Fact]
    public void GraphType_None_ReturnsNone()
    {
        // Detect doesn't support percentage, height, or volume graph
        var as1 = CreateAccountSensor(SensorType.Detect, distanceMmEmpty: null, distanceMmFull: null, capacityL: null);
        Assert.Equal(GraphType.None, as1.GraphType);
    }

    // --- EnsureEnabled ---

    [Fact]
    public void EnsureEnabled_NotDisabled_DoesNotThrow()
    {
        var as1 = CreateAccountSensor(SensorType.Level, disabled: false);
        as1.EnsureEnabled(); // should not throw
    }

    [Fact]
    public void EnsureEnabled_Disabled_ThrowsAccountSensorDisabledException()
    {
        var as1 = CreateAccountSensor(SensorType.Level, disabled: true);
        var ex = Assert.Throws<AccountSensorDisabledException>(() => as1.EnsureEnabled());
        Assert.Equal(as1.Account.Uid, ex.AccountUid);
        Assert.Equal(as1.Sensor.Uid, ex.SensorUid);
    }

    // --- HasDistance/HasHeight for various sensor types ---

    [Fact]
    public void HasDistance_Detect_ReturnsFalse()
    {
        var as1 = CreateAccountSensor(SensorType.Detect);
        Assert.False(as1.HasDistance);
    }

    [Fact]
    public void HasHeight_Moisture_ReturnsFalse()
    {
        var as1 = CreateAccountSensor(SensorType.Moisture, distanceMmEmpty: null);
        Assert.False(as1.HasHeight);
    }

    [Fact]
    public void HasHeight_Thermometer_ReturnsFalse()
    {
        var as1 = CreateAccountSensor(SensorType.Thermometer, distanceMmEmpty: null);
        Assert.False(as1.HasHeight);
    }

    // --- HasPercentage edge cases ---

    [Fact]
    public void HasPercentage_Level_FullGreaterOrEqualEmpty_ReturnsFalse()
    {
        // DistanceMmFull >= DistanceMmEmpty → no percentage
        var as1 = CreateAccountSensor(SensorType.Level, distanceMmEmpty: 800, distanceMmFull: 3000);
        Assert.False(as1.HasPercentage);
    }

    [Fact]
    public void HasTemperature_Moisture_ReturnsTrue()
    {
        var as1 = CreateAccountSensor(SensorType.Moisture);
        Assert.True(as1.HasTemperature);
    }

    [Fact]
    public void HasTemperature_Thermometer_ReturnsTrue()
    {
        var as1 = CreateAccountSensor(SensorType.Thermometer);
        Assert.True(as1.HasTemperature);
    }

    [Fact]
    public void HasConductivity_Moisture_ReturnsTrue()
    {
        var as1 = CreateAccountSensor(SensorType.Moisture);
        Assert.True(as1.HasConductivity);
    }

    [Fact]
    public void HasStatus_Detect_ReturnsTrue()
    {
        var as1 = CreateAccountSensor(SensorType.Detect);
        Assert.True(as1.HasStatus);
    }

    [Fact]
    public void HasVolume_NoCapacity_ReturnsFalse()
    {
        var as1 = CreateAccountSensor(SensorType.Level, capacityL: null);
        Assert.False(as1.HasVolume);
    }

    [Fact]
    public void HasVolume_ZeroCapacity_ReturnsFalse()
    {
        var as1 = CreateAccountSensor(SensorType.Level, capacityL: 0);
        Assert.False(as1.HasVolume);
    }
}

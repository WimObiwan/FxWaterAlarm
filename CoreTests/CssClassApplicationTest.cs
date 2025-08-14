using Core.Entities;
using Core.Util;
using Site.Utilities;
using Site.ViewComponents;
using Xunit;

namespace CoreTests;

public class CssClassApplicationTest
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
    public void TestMeasurementDisplayModel_WithOldMeasurement_HasCorrectFlags()
    {
        // Arrange
        var threshold = TimeSpan.FromHours(24);
        var oldTimestamp = DateTime.UtcNow.AddHours(-25); // 25 hours ago
        
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
        var model = new MeasurementDisplayModel<MeasurementLevelEx>
        {
            Measurement = measurementEx,
            IsOldMeasurement = measurementEx.IsOld(threshold)
        };

        // Assert
        Assert.True(model.IsOldMeasurement);
        Assert.Equal(measurementEx, model.Measurement);
        
        // Simulate the CSS class logic from the view
        var backgroundClass = model.IsOldMeasurement ? " bg-warning" : "";
        Assert.Equal(" bg-warning", backgroundClass);
    }

    [Fact]
    public void TestMeasurementDisplayModel_WithRecentMeasurement_HasCorrectFlags()
    {
        // Arrange
        var threshold = TimeSpan.FromHours(24);
        var recentTimestamp = DateTime.UtcNow.AddHours(-12); // 12 hours ago
        
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
        var model = new MeasurementDisplayModel<MeasurementLevelEx>
        {
            Measurement = measurementEx,
            IsOldMeasurement = measurementEx.IsOld(threshold)
        };

        // Assert
        Assert.False(model.IsOldMeasurement);
        Assert.Equal(measurementEx, model.Measurement);
        
        // Simulate the CSS class logic from the view
        var backgroundClass = model.IsOldMeasurement ? " bg-warning" : "";
        Assert.Equal("", backgroundClass);
    }
}
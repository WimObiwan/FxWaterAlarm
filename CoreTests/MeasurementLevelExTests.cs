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
}

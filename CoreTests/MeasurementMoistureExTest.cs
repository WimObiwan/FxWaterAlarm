using Core.Entities;
using Core.Util;
using Xunit;

namespace CoreTests;

public class MeasurementMoistureExTest
{
    private AccountSensor CreateAccountSensor()
    {
        var sensor = new Sensor
        {
            Uid = Guid.NewGuid(),
            DevEui = "devMoisture1",
            CreateTimestamp = DateTime.UtcNow,
            Type = SensorType.Moisture,
            ExpectedIntervalSecs = 3600
        };
        var account = new Account
        {
            Uid = Guid.NewGuid(),
            Email = "moisture@example.com",
            CreationTimestamp = DateTime.UtcNow
        };
        return new AccountSensor
        {
            Sensor = sensor,
            Account = account,
            CreateTimestamp = DateTime.UtcNow
        };
    }

    [Fact]
    public void Properties_ReturnCorrectValues()
    {
        var measurement = new MeasurementMoisture
        {
            DevEui = "devMoisture1",
            Timestamp = new DateTime(2025, 8, 9, 12, 0, 0, DateTimeKind.Utc),
            BatV = 3.7,
            RssiDbm = -75,
            SoilMoisturePrc = 45.5,
            SoilConductivity = 120.3,
            SoilTemperature = 22.1
        };
        var accountSensor = CreateAccountSensor();

        var ex = new MeasurementMoistureEx(measurement, accountSensor);

        Assert.Equal(45.5, ex.SoilMoisturePrc);
        Assert.Equal(120.3, ex.SoilConductivity);
        Assert.Equal(22.1, ex.SoilTemperatureC);
    }

    [Fact]
    public void BaseProperties_AreCorrect()
    {
        var measurement = new MeasurementMoisture
        {
            DevEui = "devMoisture1",
            Timestamp = new DateTime(2025, 8, 9, 12, 0, 0, DateTimeKind.Utc),
            BatV = 3.8,
            RssiDbm = -70,
            SoilMoisturePrc = 60.0,
            SoilConductivity = 200.0,
            SoilTemperature = 18.5
        };
        var accountSensor = CreateAccountSensor();

        var ex = new MeasurementMoistureEx(measurement, accountSensor);

        Assert.Equal("devMoisture1", ex.DevEui);
        Assert.Equal(new DateTime(2025, 8, 9, 12, 0, 0, DateTimeKind.Utc), ex.Timestamp);
        Assert.Equal(3.8, ex.BatV);
        Assert.Equal(-70, ex.RssiDbm);
        Assert.Equal(accountSensor, ex.AccountSensor);
    }

    [Fact]
    public void GetValues_ReturnsDictionaryWithAllMoistureValues()
    {
        var measurement = new MeasurementMoisture
        {
            DevEui = "devMoisture1",
            Timestamp = DateTime.UtcNow,
            BatV = 3.7,
            RssiDbm = -75,
            SoilMoisturePrc = 45.5,
            SoilConductivity = 120.3,
            SoilTemperature = 22.1
        };
        var accountSensor = CreateAccountSensor();

        var ex = new MeasurementMoistureEx(measurement, accountSensor);

        var values = ex.GetValues();
        Assert.Equal(3, values.Count);
        Assert.Equal(45.5, values["SoilMoisturePrc"]);
        Assert.Equal(120.3, values["SoilConductivity"]);
        Assert.Equal(22.1, values["SoilTemperature"]);
    }

    [Fact]
    public void Properties_WithZeroValues()
    {
        var measurement = new MeasurementMoisture
        {
            DevEui = "devMoisture1",
            Timestamp = DateTime.UtcNow,
            BatV = 3.0,
            RssiDbm = -150,
            SoilMoisturePrc = 0.0,
            SoilConductivity = 0.0,
            SoilTemperature = 0.0
        };
        var accountSensor = CreateAccountSensor();

        var ex = new MeasurementMoistureEx(measurement, accountSensor);

        Assert.Equal(0.0, ex.SoilMoisturePrc);
        Assert.Equal(0.0, ex.SoilConductivity);
        Assert.Equal(0.0, ex.SoilTemperatureC);
    }
}

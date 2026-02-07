using Core.Entities;
using Core.Util;
using Xunit;

namespace CoreTests.Util;

public class MeasurementThermometerExTest
{
    private AccountSensor CreateAccountSensor()
    {
        var sensor = new Sensor
        {
            Uid = Guid.NewGuid(),
            DevEui = "devThermo1",
            CreateTimestamp = DateTime.UtcNow,
            Type = SensorType.Thermometer,
            ExpectedIntervalSecs = 3600
        };
        var account = new Account
        {
            Uid = Guid.NewGuid(),
            Email = "thermo@example.com",
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
        var measurement = new MeasurementThermometer
        {
            DevEui = "devThermo1",
            Timestamp = new DateTime(2025, 8, 9, 12, 0, 0, DateTimeKind.Utc),
            BatV = 3.6,
            RssiDbm = -80,
            TempC = 23.5,
            HumPrc = 65.0
        };
        var accountSensor = CreateAccountSensor();

        var ex = new MeasurementThermometerEx(measurement, accountSensor);

        Assert.Equal(23.5, ex.TempC);
        Assert.Equal(65.0, ex.HumPrc);
    }

    [Fact]
    public void BaseProperties_AreCorrect()
    {
        var measurement = new MeasurementThermometer
        {
            DevEui = "devThermo1",
            Timestamp = new DateTime(2025, 8, 9, 12, 0, 0, DateTimeKind.Utc),
            BatV = 3.9,
            RssiDbm = -60,
            TempC = 18.0,
            HumPrc = 50.0
        };
        var accountSensor = CreateAccountSensor();

        var ex = new MeasurementThermometerEx(measurement, accountSensor);

        Assert.Equal("devThermo1", ex.DevEui);
        Assert.Equal(new DateTime(2025, 8, 9, 12, 0, 0, DateTimeKind.Utc), ex.Timestamp);
        Assert.Equal(3.9, ex.BatV);
        Assert.Equal(-60, ex.RssiDbm);
        Assert.Equal(accountSensor, ex.AccountSensor);
    }

    [Fact]
    public void GetValues_ReturnsDictionaryWithTempAndHumidity()
    {
        var measurement = new MeasurementThermometer
        {
            DevEui = "devThermo1",
            Timestamp = DateTime.UtcNow,
            BatV = 3.6,
            RssiDbm = -80,
            TempC = 23.5,
            HumPrc = 65.0
        };
        var accountSensor = CreateAccountSensor();

        var ex = new MeasurementThermometerEx(measurement, accountSensor);

        var values = ex.GetValues();
        Assert.Equal(2, values.Count);
        Assert.Equal(23.5, values["TempC"]);
        Assert.Equal(65.0, values["HumPrc"]);
    }

    [Fact]
    public void Properties_NegativeTemperature()
    {
        var measurement = new MeasurementThermometer
        {
            DevEui = "devThermo1",
            Timestamp = DateTime.UtcNow,
            BatV = 3.3,
            RssiDbm = -100,
            TempC = -15.5,
            HumPrc = 95.0
        };
        var accountSensor = CreateAccountSensor();

        var ex = new MeasurementThermometerEx(measurement, accountSensor);

        Assert.Equal(-15.5, ex.TempC);
        Assert.Equal(95.0, ex.HumPrc);
    }
}

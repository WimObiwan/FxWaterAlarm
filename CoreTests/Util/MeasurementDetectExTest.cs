using Core.Entities;
using Core.Util;
using Xunit;

namespace CoreTests.Util;

public class MeasurementDetectExTest
{
    private AccountSensor CreateAccountSensor()
    {
        var sensor = new Sensor
        {
            Uid = Guid.NewGuid(),
            DevEui = "devDetect1",
            CreateTimestamp = DateTime.UtcNow,
            Type = SensorType.Detect,
            ExpectedIntervalSecs = 3600
        };
        var account = new Account
        {
            Uid = Guid.NewGuid(),
            Email = "detect@example.com",
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
    public void Status_ReturnsCorrectValue()
    {
        var measurement = new MeasurementDetect
        {
            DevEui = "devDetect1",
            Timestamp = new DateTime(2025, 8, 9, 12, 0, 0, DateTimeKind.Utc),
            BatV = 3.6,
            RssiDbm = -80,
            Status = 1
        };
        var accountSensor = CreateAccountSensor();

        var ex = new MeasurementDetectEx(measurement, accountSensor);

        Assert.Equal(1, ex.Status);
    }

    [Fact]
    public void Status_ZeroStatus()
    {
        var measurement = new MeasurementDetect
        {
            DevEui = "devDetect1",
            Timestamp = new DateTime(2025, 8, 9, 12, 0, 0, DateTimeKind.Utc),
            BatV = 3.6,
            RssiDbm = -80,
            Status = 0
        };
        var accountSensor = CreateAccountSensor();

        var ex = new MeasurementDetectEx(measurement, accountSensor);

        Assert.Equal(0, ex.Status);
    }

    [Fact]
    public void BaseProperties_AreCorrect()
    {
        var measurement = new MeasurementDetect
        {
            DevEui = "devDetect1",
            Timestamp = new DateTime(2025, 8, 9, 12, 0, 0, DateTimeKind.Utc),
            BatV = 3.5,
            RssiDbm = -90,
            Status = 1
        };
        var accountSensor = CreateAccountSensor();

        var ex = new MeasurementDetectEx(measurement, accountSensor);

        Assert.Equal("devDetect1", ex.DevEui);
        Assert.Equal(new DateTime(2025, 8, 9, 12, 0, 0, DateTimeKind.Utc), ex.Timestamp);
        Assert.Equal(3.5, ex.BatV);
        Assert.Equal(-90, ex.RssiDbm);
        Assert.Equal(accountSensor, ex.AccountSensor);
    }

    [Fact]
    public void GetValues_ReturnsDictionaryWithStatus()
    {
        var measurement = new MeasurementDetect
        {
            DevEui = "devDetect1",
            Timestamp = DateTime.UtcNow,
            BatV = 3.6,
            RssiDbm = -80,
            Status = 1
        };
        var accountSensor = CreateAccountSensor();

        var ex = new MeasurementDetectEx(measurement, accountSensor);

        var values = ex.GetValues();
        Assert.Single(values);
        Assert.Equal(1, values["Status"]);
    }
}

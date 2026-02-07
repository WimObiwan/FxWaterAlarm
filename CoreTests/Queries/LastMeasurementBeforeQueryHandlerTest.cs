using Core.Entities;
using Core.Queries;
using Core.Util;
using System.Reflection;
using Xunit;

namespace CoreTests.Queries;

public class LastMeasurementBeforeQueryHandlerTest
{
    private static AccountSensor CreateAccountSensor(SensorType type)
    {
        var accountSensor = new AccountSensor
        {
            Account = new Account
            {
                Uid = Guid.NewGuid(), Email = "t@t.com",
                CreationTimestamp = DateTime.UtcNow, Link = "al"
            },
            Sensor = new Sensor
            {
                Uid = Guid.NewGuid(), DevEui = "dev123",
                CreateTimestamp = DateTime.UtcNow, Type = type, Link = "sl"
            },
            CreateTimestamp = DateTime.UtcNow
        };
        var alarmsField = typeof(AccountSensor).GetField("_alarms", BindingFlags.NonPublic | BindingFlags.Instance);
        alarmsField?.SetValue(accountSensor, new List<AccountSensorAlarm>());
        return accountSensor;
    }

    [Fact]
    public async Task Handle_Level_ReturnsMeasurementLevelEx()
    {
        var levelRepo = new FakeMeasurementLevelRepository
        {
            LastBeforeResult = new MeasurementLevel { DevEui = "dev123", DistanceMm = 1500, BatV = 3.5, RssiDbm = -80 }
        };
        var handler = new LastMeasurementBeforeQueryHandler(levelRepo);
        var ts = DateTime.UtcNow;

        var result = await handler.Handle(
            new LastMeasurementBeforeQuery { AccountSensor = CreateAccountSensor(SensorType.Level), Timestamp = ts },
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.IsType<MeasurementLevelEx>(result);
    }

    [Fact]
    public async Task Handle_LevelPressure_ReturnsMeasurementLevelEx()
    {
        var levelRepo = new FakeMeasurementLevelRepository
        {
            LastBeforeResult = new MeasurementLevel { DevEui = "dev123", DistanceMm = 1500, BatV = 3.5, RssiDbm = -80 }
        };
        var handler = new LastMeasurementBeforeQueryHandler(levelRepo);

        var result = await handler.Handle(
            new LastMeasurementBeforeQuery { AccountSensor = CreateAccountSensor(SensorType.LevelPressure), Timestamp = DateTime.UtcNow },
            CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Handle_Level_NullMeasurement_ReturnsNull()
    {
        var levelRepo = new FakeMeasurementLevelRepository { LastBeforeResult = null };
        var handler = new LastMeasurementBeforeQueryHandler(levelRepo);

        var result = await handler.Handle(
            new LastMeasurementBeforeQuery { AccountSensor = CreateAccountSensor(SensorType.Level), Timestamp = DateTime.UtcNow },
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_Detect_ReturnsNull()
    {
        var handler = new LastMeasurementBeforeQueryHandler(new FakeMeasurementLevelRepository());

        var result = await handler.Handle(
            new LastMeasurementBeforeQuery { AccountSensor = CreateAccountSensor(SensorType.Detect), Timestamp = DateTime.UtcNow },
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_Moisture_ReturnsNull()
    {
        var handler = new LastMeasurementBeforeQueryHandler(new FakeMeasurementLevelRepository());

        var result = await handler.Handle(
            new LastMeasurementBeforeQuery { AccountSensor = CreateAccountSensor(SensorType.Moisture), Timestamp = DateTime.UtcNow },
            CancellationToken.None);

        Assert.Null(result);
    }
}

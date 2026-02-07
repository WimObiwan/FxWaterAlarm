using Core.Entities;
using Core.Queries;
using Core.Util;
using System.Reflection;
using Xunit;

namespace CoreTests.Queries;

public class LastMeasurementQueryHandlerTest
{
    private static AccountSensor CreateAccountSensor(SensorType type, string devEui = "dev123")
    {
        var accountSensor = new AccountSensor
        {
            Account = new Account
            {
                Uid = Guid.NewGuid(),
                Email = "t@t.com",
                CreationTimestamp = DateTime.UtcNow,
                Link = "al"
            },
            Sensor = new Sensor
            {
                Uid = Guid.NewGuid(),
                DevEui = devEui,
                CreateTimestamp = DateTime.UtcNow,
                Type = type,
                Link = "sl"
            },
            CreateTimestamp = DateTime.UtcNow
        };

        // Initialize private _alarms field
        var alarmsField = typeof(AccountSensor).GetField("_alarms", BindingFlags.NonPublic | BindingFlags.Instance);
        alarmsField?.SetValue(accountSensor, new List<AccountSensorAlarm>());

        return accountSensor;
    }

    [Fact]
    public async Task Handle_Level_ReturnsMeasurementLevelEx()
    {
        var levelRepo = new FakeMeasurementLevelRepository
        {
            LastResult = new MeasurementLevel { DevEui = "dev123", DistanceMm = 1500, BatV = 3.5, RssiDbm = -80 }
        };
        var handler = new LastMeasurementQueryHandler(
            levelRepo, new FakeMeasurementDetectRepository(),
            new FakeMeasurementMoistureRepository(), new FakeMeasurementThermometerRepository());

        var result = await handler.Handle(
            new LastMeasurementQuery { AccountSensor = CreateAccountSensor(SensorType.Level) },
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.IsType<MeasurementLevelEx>(result);
    }

    [Fact]
    public async Task Handle_LevelPressure_ReturnsMeasurementLevelEx()
    {
        var levelRepo = new FakeMeasurementLevelRepository
        {
            LastResult = new MeasurementLevel { DevEui = "dev123", DistanceMm = 1500, BatV = 3.5, RssiDbm = -80 }
        };
        var handler = new LastMeasurementQueryHandler(
            levelRepo, new FakeMeasurementDetectRepository(),
            new FakeMeasurementMoistureRepository(), new FakeMeasurementThermometerRepository());

        var result = await handler.Handle(
            new LastMeasurementQuery { AccountSensor = CreateAccountSensor(SensorType.LevelPressure) },
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.IsType<MeasurementLevelEx>(result);
    }

    [Fact]
    public async Task Handle_Level_NullMeasurement_ReturnsNull()
    {
        var levelRepo = new FakeMeasurementLevelRepository { LastResult = null };
        var handler = new LastMeasurementQueryHandler(
            levelRepo, new FakeMeasurementDetectRepository(),
            new FakeMeasurementMoistureRepository(), new FakeMeasurementThermometerRepository());

        var result = await handler.Handle(
            new LastMeasurementQuery { AccountSensor = CreateAccountSensor(SensorType.Level) },
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_Detect_ReturnsMeasurementDetectEx()
    {
        var detectRepo = new FakeMeasurementDetectRepository
        {
            LastResult = new MeasurementDetect { DevEui = "dev123", Status = 1, BatV = 3.5, RssiDbm = -80 }
        };
        var handler = new LastMeasurementQueryHandler(
            new FakeMeasurementLevelRepository(), detectRepo,
            new FakeMeasurementMoistureRepository(), new FakeMeasurementThermometerRepository());

        var result = await handler.Handle(
            new LastMeasurementQuery { AccountSensor = CreateAccountSensor(SensorType.Detect) },
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.IsType<MeasurementDetectEx>(result);
    }

    [Fact]
    public async Task Handle_Detect_NullMeasurement_ReturnsNull()
    {
        var detectRepo = new FakeMeasurementDetectRepository { LastResult = null };
        var handler = new LastMeasurementQueryHandler(
            new FakeMeasurementLevelRepository(), detectRepo,
            new FakeMeasurementMoistureRepository(), new FakeMeasurementThermometerRepository());

        var result = await handler.Handle(
            new LastMeasurementQuery { AccountSensor = CreateAccountSensor(SensorType.Detect) },
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_Moisture_ReturnsMeasurementMoistureEx()
    {
        var moistureRepo = new FakeMeasurementMoistureRepository
        {
            LastResult = new MeasurementMoisture
            {
                DevEui = "dev123", SoilMoisturePrc = 50.0, SoilConductivity = 100,
                SoilTemperature = 20, BatV = 3.5, RssiDbm = -80
            }
        };
        var handler = new LastMeasurementQueryHandler(
            new FakeMeasurementLevelRepository(), new FakeMeasurementDetectRepository(),
            moistureRepo, new FakeMeasurementThermometerRepository());

        var result = await handler.Handle(
            new LastMeasurementQuery { AccountSensor = CreateAccountSensor(SensorType.Moisture) },
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.IsType<MeasurementMoistureEx>(result);
    }

    [Fact]
    public async Task Handle_Moisture_NullMeasurement_ReturnsNull()
    {
        var moistureRepo = new FakeMeasurementMoistureRepository { LastResult = null };
        var handler = new LastMeasurementQueryHandler(
            new FakeMeasurementLevelRepository(), new FakeMeasurementDetectRepository(),
            moistureRepo, new FakeMeasurementThermometerRepository());

        var result = await handler.Handle(
            new LastMeasurementQuery { AccountSensor = CreateAccountSensor(SensorType.Moisture) },
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_Thermometer_ReturnsMeasurementThermometerEx()
    {
        var thermoRepo = new FakeMeasurementThermometerRepository
        {
            LastResult = new MeasurementThermometer
            {
                DevEui = "dev123", TempC = 22.5, HumPrc = 60.0, BatV = 3.5, RssiDbm = -80
            }
        };
        var handler = new LastMeasurementQueryHandler(
            new FakeMeasurementLevelRepository(), new FakeMeasurementDetectRepository(),
            new FakeMeasurementMoistureRepository(), thermoRepo);

        var result = await handler.Handle(
            new LastMeasurementQuery { AccountSensor = CreateAccountSensor(SensorType.Thermometer) },
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.IsType<MeasurementThermometerEx>(result);
    }

    [Fact]
    public async Task Handle_Thermometer_NullMeasurement_ReturnsNull()
    {
        var thermoRepo = new FakeMeasurementThermometerRepository { LastResult = null };
        var handler = new LastMeasurementQueryHandler(
            new FakeMeasurementLevelRepository(), new FakeMeasurementDetectRepository(),
            new FakeMeasurementMoistureRepository(), thermoRepo);

        var result = await handler.Handle(
            new LastMeasurementQuery { AccountSensor = CreateAccountSensor(SensorType.Thermometer) },
            CancellationToken.None);

        Assert.Null(result);
    }
}

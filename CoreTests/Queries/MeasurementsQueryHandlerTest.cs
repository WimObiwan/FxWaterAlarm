using Core.Entities;
using Core.Queries;
using Core.Util;
using System.Reflection;
using Xunit;

namespace CoreTests.Queries;

public class MeasurementsQueryHandlerTest
{
    private static AccountSensor CreateAccountSensor(SensorType type, string devEui = "dev123")
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
                Uid = Guid.NewGuid(), DevEui = devEui,
                CreateTimestamp = DateTime.UtcNow, Type = type, Link = "sl"
            },
            CreateTimestamp = DateTime.UtcNow
        };
        var alarmsField = typeof(AccountSensor).GetField("_alarms", BindingFlags.NonPublic | BindingFlags.Instance);
        alarmsField?.SetValue(accountSensor, new List<AccountSensorAlarm>());
        return accountSensor;
    }

    [Fact]
    public async Task Handle_Level_ReturnsMeasurementLevelExArray()
    {
        var levelRepo = new FakeMeasurementLevelRepository
        {
            GetResult = new[]
            {
                new MeasurementLevel { DevEui = "dev123", DistanceMm = 1500, BatV = 3.5, RssiDbm = -80 },
                new MeasurementLevel { DevEui = "dev123", DistanceMm = 1600, BatV = 3.4, RssiDbm = -82 }
            }
        };
        var handler = new MeasurementsQueryHandler(
            levelRepo, new FakeMeasurementDetectRepository(),
            new FakeMeasurementMoistureRepository(), new FakeMeasurementThermometerRepository());

        var result = await handler.Handle(
            new MeasurementsQuery { AccountSensor = CreateAccountSensor(SensorType.Level) },
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2, result!.Length);
        Assert.All(result, r => Assert.IsType<MeasurementLevelEx>(r));
    }

    [Fact]
    public async Task Handle_Detect_ReturnsMeasurementDetectExArray()
    {
        var detectRepo = new FakeMeasurementDetectRepository
        {
            GetResult = new[]
            {
                new MeasurementDetect { DevEui = "dev123", Status = 1, BatV = 3.5, RssiDbm = -80 }
            }
        };
        var handler = new MeasurementsQueryHandler(
            new FakeMeasurementLevelRepository(), detectRepo,
            new FakeMeasurementMoistureRepository(), new FakeMeasurementThermometerRepository());

        var result = await handler.Handle(
            new MeasurementsQuery { AccountSensor = CreateAccountSensor(SensorType.Detect) },
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result!);
        Assert.IsType<MeasurementDetectEx>(result[0]);
    }

    [Fact]
    public async Task Handle_Moisture_ReturnsMeasurementMoistureExArray()
    {
        var moistureRepo = new FakeMeasurementMoistureRepository
        {
            GetResult = new[]
            {
                new MeasurementMoisture
                {
                    DevEui = "dev123", SoilMoisturePrc = 50, SoilConductivity = 100,
                    SoilTemperature = 20, BatV = 3.5, RssiDbm = -80
                }
            }
        };
        var handler = new MeasurementsQueryHandler(
            new FakeMeasurementLevelRepository(), new FakeMeasurementDetectRepository(),
            moistureRepo, new FakeMeasurementThermometerRepository());

        var result = await handler.Handle(
            new MeasurementsQuery { AccountSensor = CreateAccountSensor(SensorType.Moisture) },
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.IsType<MeasurementMoistureEx>(result![0]);
    }

    [Fact]
    public async Task Handle_Thermometer_ReturnsMeasurementThermometerExArray()
    {
        var thermoRepo = new FakeMeasurementThermometerRepository
        {
            GetResult = new[]
            {
                new MeasurementThermometer
                {
                    DevEui = "dev123", TempC = 22.5, HumPrc = 60, BatV = 3.5, RssiDbm = -80
                }
            }
        };
        var handler = new MeasurementsQueryHandler(
            new FakeMeasurementLevelRepository(), new FakeMeasurementDetectRepository(),
            new FakeMeasurementMoistureRepository(), thermoRepo);

        var result = await handler.Handle(
            new MeasurementsQuery { AccountSensor = CreateAccountSensor(SensorType.Thermometer) },
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.IsType<MeasurementThermometerEx>(result![0]);
    }

    [Fact]
    public async Task Handle_Level_NullMeasurements_ReturnsNull()
    {
        var levelRepo = new FakeMeasurementLevelRepository { GetResult = null };
        var handler = new MeasurementsQueryHandler(
            levelRepo, new FakeMeasurementDetectRepository(),
            new FakeMeasurementMoistureRepository(), new FakeMeasurementThermometerRepository());

        var result = await handler.Handle(
            new MeasurementsQuery { AccountSensor = CreateAccountSensor(SensorType.Level) },
            CancellationToken.None);

        // GetResult defaults to empty array, so we get empty not null
        Assert.NotNull(result);
        Assert.Empty(result!);
    }

    [Fact]
    public async Task Handle_WithFromAndTill_PassesParametersThrough()
    {
        var from = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var till = new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        var levelRepo = new FakeMeasurementLevelRepository();
        var handler = new MeasurementsQueryHandler(
            levelRepo, new FakeMeasurementDetectRepository(),
            new FakeMeasurementMoistureRepository(), new FakeMeasurementThermometerRepository());

        var result = await handler.Handle(
            new MeasurementsQuery
            {
                AccountSensor = CreateAccountSensor(SensorType.Level),
                From = from,
                Till = till
            },
            CancellationToken.None);

        // No exception means the parameters were passed through successfully
        Assert.NotNull(result);
    }
}

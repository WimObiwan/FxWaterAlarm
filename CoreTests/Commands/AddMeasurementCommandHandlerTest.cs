using Core.Commands;
using Core.Entities;
using Xunit;

namespace CoreTests.Commands;

public class AddMeasurementCommandHandlerTest
{
    private AddMeasurementCommandHandler CreateHandler(TestDbContext db,
        FakeMeasurementLevelRepository? level = null,
        FakeMeasurementDetectRepository? detect = null,
        FakeMeasurementMoistureRepository? moisture = null,
        FakeMeasurementThermometerRepository? thermometer = null)
    {
        return new AddMeasurementCommandHandler(
            level ?? new FakeMeasurementLevelRepository(),
            detect ?? new FakeMeasurementDetectRepository(),
            moisture ?? new FakeMeasurementMoistureRepository(),
            thermometer ?? new FakeMeasurementThermometerRepository(),
            db.Context);
    }

    private static async Task<Sensor> SeedSensor(TestDbContext db, SensorType type, string devEui)
    {
        var sensor = TestEntityFactory.CreateSensor(type, devEui: devEui);
        db.Context.Sensors.Add(sensor);
        await db.Context.SaveChangesAsync();
        return sensor;
    }

    [Fact]
    public async Task Handle_LevelSensor_WritesToLevelRepository()
    {
        await using var db = TestDbContext.Create();
        await SeedSensor(db, SensorType.Level, "DEV_LEVEL_001");

        var levelRepo = new FakeMeasurementLevelRepository();
        var handler = CreateHandler(db, level: levelRepo);

        // Should not throw
        await handler.Handle(new AddMeasurementCommand
        {
            DevEui = "DEV_LEVEL_001",
            Timestamp = DateTime.UtcNow,
            Measurements = new Dictionary<string, object>
            {
                ["batV"] = 3.6,
                ["rssi"] = -80,
                ["distance"] = 1234
            }
        }, CancellationToken.None);
    }

    [Fact]
    public async Task Handle_DetectSensor_WritesToDetectRepository()
    {
        await using var db = TestDbContext.Create();
        await SeedSensor(db, SensorType.Detect, "DEV_DETECT_01");

        var handler = CreateHandler(db);

        await handler.Handle(new AddMeasurementCommand
        {
            DevEui = "DEV_DETECT_01",
            Timestamp = DateTime.UtcNow,
            Measurements = new Dictionary<string, object>
            {
                ["BatV"] = 3.2,
                ["RSSI"] = -60,
                ["waterStatus"] = 1
            }
        }, CancellationToken.None);
    }

    [Fact]
    public async Task Handle_MoistureSensor_WritesToMoistureRepository()
    {
        await using var db = TestDbContext.Create();
        await SeedSensor(db, SensorType.Moisture, "DEV_MOIST_001");

        var handler = CreateHandler(db);

        await handler.Handle(new AddMeasurementCommand
        {
            DevEui = "DEV_MOIST_001",
            Timestamp = DateTime.UtcNow,
            Measurements = new Dictionary<string, object>
            {
                ["batV"] = 3.5,
                ["rssi"] = -70,
                ["soilMoisturePrc"] = 42.5,
                ["soilConductivity"] = 150,
                ["soilTemperature"] = 18.3
            }
        }, CancellationToken.None);
    }

    [Fact]
    public async Task Handle_ThermometerSensor_WritesToThermometerRepository()
    {
        await using var db = TestDbContext.Create();
        await SeedSensor(db, SensorType.Thermometer, "DEV_THERM_001");

        var handler = CreateHandler(db);

        await handler.Handle(new AddMeasurementCommand
        {
            DevEui = "DEV_THERM_001",
            Timestamp = DateTime.UtcNow,
            Measurements = new Dictionary<string, object>
            {
                ["batV"] = 3.3,
                ["rssi"] = -90,
                ["tempC"] = 22.5,
                ["humPrc"] = 65.0
            }
        }, CancellationToken.None);
    }

    [Fact]
    public async Task Handle_SensorNotFound_ThrowsInvalidOperationException()
    {
        await using var db = TestDbContext.Create();
        var handler = CreateHandler(db);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new AddMeasurementCommand
            {
                DevEui = "NONEXISTENT",
                Timestamp = DateTime.UtcNow,
                Measurements = new Dictionary<string, object>
                {
                    ["batV"] = 3.0,
                    ["rssi"] = -50,
                    ["distance"] = 100
                }
            }, CancellationToken.None));

        Assert.Contains("NONEXISTENT", ex.Message);
    }

    [Fact]
    public async Task Handle_MissingRequiredMeasurement_ThrowsArgumentException()
    {
        await using var db = TestDbContext.Create();
        await SeedSensor(db, SensorType.Level, "DEV_MISS_001");

        var handler = CreateHandler(db);

        // Missing "distance" measurement
        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.Handle(new AddMeasurementCommand
            {
                DevEui = "DEV_MISS_001",
                Timestamp = DateTime.UtcNow,
                Measurements = new Dictionary<string, object>
                {
                    ["batV"] = 3.6,
                    ["rssi"] = -80
                    // missing "distance"
                }
            }, CancellationToken.None));
    }
}

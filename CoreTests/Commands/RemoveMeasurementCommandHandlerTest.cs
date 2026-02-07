using Core.Commands;
using Core.Configuration;
using Core.Entities;
using Microsoft.Extensions.Options;
using Xunit;

namespace CoreTests.Commands;

public class RemoveMeasurementCommandHandlerTest
{
    private static RemoveMeasurementCommandHandler CreateHandler(TestDbContext db,
        FakeMeasurementLevelRepository? level = null,
        FakeMeasurementDetectRepository? detect = null,
        FakeMeasurementMoistureRepository? moisture = null,
        FakeMeasurementThermometerRepository? thermometer = null,
        int toleranceSecs = 5)
    {
        return new RemoveMeasurementCommandHandler(
            level ?? new FakeMeasurementLevelRepository(),
            detect ?? new FakeMeasurementDetectRepository(),
            moisture ?? new FakeMeasurementMoistureRepository(),
            thermometer ?? new FakeMeasurementThermometerRepository(),
            db.Context,
            Options.Create(new MeasurementRemovalOptions { TimestampToleranceSeconds = toleranceSecs }));
    }

    [Fact]
    public async Task Handle_LevelSensor_SingleMeasurement_Succeeds()
    {
        await using var db = TestDbContext.Create();
        var sensor = TestEntityFactory.CreateSensor(SensorType.Level, devEui: "RM_LEVEL_01");
        db.Context.Sensors.Add(sensor);
        await db.Context.SaveChangesAsync();

        var levelRepo = new FakeMeasurementLevelRepository
        {
            GetResult = [new MeasurementLevel { DevEui = "RM_LEVEL_01", Timestamp = DateTime.UtcNow }]
        };

        var handler = CreateHandler(db, level: levelRepo);

        // Should not throw — single measurement found in range
        await handler.Handle(new RemoveMeasurementCommand
        {
            SensorUid = sensor.Uid,
            Timestamp = DateTime.UtcNow
        }, CancellationToken.None);
    }

    [Fact]
    public async Task Handle_DetectSensor_SingleMeasurement_Succeeds()
    {
        await using var db = TestDbContext.Create();
        var sensor = TestEntityFactory.CreateSensor(SensorType.Detect, devEui: "RM_DET_001");
        db.Context.Sensors.Add(sensor);
        await db.Context.SaveChangesAsync();

        var detectRepo = new FakeMeasurementDetectRepository
        {
            GetResult = [new MeasurementDetect { DevEui = "RM_DET_001", Timestamp = DateTime.UtcNow }]
        };

        var handler = CreateHandler(db, detect: detectRepo);

        await handler.Handle(new RemoveMeasurementCommand
        {
            SensorUid = sensor.Uid,
            Timestamp = DateTime.UtcNow
        }, CancellationToken.None);
    }

    [Fact]
    public async Task Handle_SensorNotFound_ThrowsInvalidOperationException()
    {
        await using var db = TestDbContext.Create();
        var handler = CreateHandler(db);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new RemoveMeasurementCommand
            {
                SensorUid = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow
            }, CancellationToken.None));

        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public async Task Handle_NoMeasurementsInRange_ThrowsInvalidOperationException()
    {
        await using var db = TestDbContext.Create();
        var sensor = TestEntityFactory.CreateSensor(SensorType.Level, devEui: "RM_EMPTY_01");
        db.Context.Sensors.Add(sensor);
        await db.Context.SaveChangesAsync();

        var levelRepo = new FakeMeasurementLevelRepository
        {
            GetResult = [] // empty
        };

        var handler = CreateHandler(db, level: levelRepo);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new RemoveMeasurementCommand
            {
                SensorUid = sensor.Uid,
                Timestamp = DateTime.UtcNow
            }, CancellationToken.None));

        Assert.Contains("No measurements", ex.Message);
    }

    [Fact]
    public async Task Handle_MultipleMeasurementsInRange_ThrowsInvalidOperationException()
    {
        await using var db = TestDbContext.Create();
        var sensor = TestEntityFactory.CreateSensor(SensorType.Level, devEui: "RM_MULTI_01");
        db.Context.Sensors.Add(sensor);
        await db.Context.SaveChangesAsync();

        var levelRepo = new FakeMeasurementLevelRepository
        {
            GetResult =
            [
                new MeasurementLevel { DevEui = "RM_MULTI_01", Timestamp = DateTime.UtcNow },
                new MeasurementLevel { DevEui = "RM_MULTI_01", Timestamp = DateTime.UtcNow.AddSeconds(1) }
            ]
        };

        var handler = CreateHandler(db, level: levelRepo);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new RemoveMeasurementCommand
            {
                SensorUid = sensor.Uid,
                Timestamp = DateTime.UtcNow
            }, CancellationToken.None));

        Assert.Contains("Multiple", ex.Message);
    }
}

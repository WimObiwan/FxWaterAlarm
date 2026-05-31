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
    public async Task Handle_MultipleMeasurementsInRange_DeletesClosestMeasurement()
    {
        await using var db = TestDbContext.Create();
        var sensor = TestEntityFactory.CreateSensor(SensorType.Level, devEui: "RM_MULTI_01");
        db.Context.Sensors.Add(sensor);
        await db.Context.SaveChangesAsync();

        var requestedTimestamp = DateTime.UtcNow;
        var closestTimestamp = requestedTimestamp.AddMilliseconds(300);
        var fartherTimestamp = requestedTimestamp.AddSeconds(2);

        var levelRepo = new FakeMeasurementLevelRepository
        {
            GetResult =
            [
                new MeasurementLevel { DevEui = "RM_MULTI_01", Timestamp = fartherTimestamp },
                new MeasurementLevel { DevEui = "RM_MULTI_01", Timestamp = closestTimestamp }
            ]
        };

        var handler = CreateHandler(db, level: levelRepo);

        await handler.Handle(new RemoveMeasurementCommand
        {
            SensorUid = sensor.Uid,
            Timestamp = requestedTimestamp
        }, CancellationToken.None);

        Assert.Equal(closestTimestamp, levelRepo.LastDeleteFrom);
        Assert.Equal(closestTimestamp, levelRepo.LastDeleteTill);
    }

    [Fact]
    public async Task Handle_EquallyCloseMeasurements_ThrowsInvalidOperationException()
    {
        await using var db = TestDbContext.Create();
        var sensor = TestEntityFactory.CreateSensor(SensorType.Level, devEui: "RM_EQUAL_01");
        db.Context.Sensors.Add(sensor);
        await db.Context.SaveChangesAsync();

        var requestedTimestamp = DateTime.UtcNow;
        var before = requestedTimestamp.AddMilliseconds(-500);
        var after = requestedTimestamp.AddMilliseconds(500);

        var levelRepo = new FakeMeasurementLevelRepository
        {
            GetResult =
            [
                new MeasurementLevel { DevEui = "RM_EQUAL_01", Timestamp = before },
                new MeasurementLevel { DevEui = "RM_EQUAL_01", Timestamp = after }
            ]
        };

        var handler = CreateHandler(db, level: levelRepo);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new RemoveMeasurementCommand
            {
                SensorUid = sensor.Uid,
                Timestamp = requestedTimestamp
            }, CancellationToken.None));

        Assert.Contains("equally close", ex.Message);
        Assert.Null(levelRepo.LastDeleteFrom);
        Assert.Null(levelRepo.LastDeleteTill);
    }

    [Fact]
    public async Task Handle_LevelPressureSensor_SingleMeasurement_UsesLevelRepository()
    {
        await using var db = TestDbContext.Create();
        var sensor = TestEntityFactory.CreateSensor(SensorType.LevelPressure, devEui: "RM_LP_01");
        db.Context.Sensors.Add(sensor);
        await db.Context.SaveChangesAsync();

        var requestedTimestamp = DateTime.UtcNow;
        var levelRepo = new FakeMeasurementLevelRepository
        {
            GetResult = [new MeasurementLevel { DevEui = "RM_LP_01", Timestamp = requestedTimestamp }]
        };

        var handler = CreateHandler(db, level: levelRepo);

        await handler.Handle(new RemoveMeasurementCommand
        {
            SensorUid = sensor.Uid,
            Timestamp = requestedTimestamp
        }, CancellationToken.None);

        Assert.Equal(requestedTimestamp, levelRepo.LastDeleteFrom);
        Assert.Equal(requestedTimestamp, levelRepo.LastDeleteTill);
    }

    [Fact]
    public async Task Handle_DetectSensor_MultipleMeasurements_DeletesClosestMeasurement()
    {
        await using var db = TestDbContext.Create();
        var sensor = TestEntityFactory.CreateSensor(SensorType.Detect, devEui: "RM_DET_MULTI_01");
        db.Context.Sensors.Add(sensor);
        await db.Context.SaveChangesAsync();

        var requestedTimestamp = DateTime.UtcNow;
        var closestTimestamp = requestedTimestamp.AddMilliseconds(150);
        var fartherTimestamp = requestedTimestamp.AddSeconds(-2);

        var detectRepo = new FakeMeasurementDetectRepository
        {
            GetResult =
            [
                new MeasurementDetect { DevEui = "RM_DET_MULTI_01", Timestamp = fartherTimestamp },
                new MeasurementDetect { DevEui = "RM_DET_MULTI_01", Timestamp = closestTimestamp }
            ]
        };

        var handler = CreateHandler(db, detect: detectRepo);

        await handler.Handle(new RemoveMeasurementCommand
        {
            SensorUid = sensor.Uid,
            Timestamp = requestedTimestamp
        }, CancellationToken.None);

        Assert.Equal(closestTimestamp, detectRepo.LastDeleteFrom);
        Assert.Equal(closestTimestamp, detectRepo.LastDeleteTill);
    }

    [Fact]
    public async Task Handle_MoistureSensor_SingleMeasurement_DeletesMeasurement()
    {
        await using var db = TestDbContext.Create();
        var sensor = TestEntityFactory.CreateSensor(SensorType.Moisture, devEui: "RM_MOI_01");
        db.Context.Sensors.Add(sensor);
        await db.Context.SaveChangesAsync();

        var requestedTimestamp = DateTime.UtcNow;
        var moistureRepo = new FakeMeasurementMoistureRepository
        {
            GetResult = [new MeasurementMoisture { DevEui = "RM_MOI_01", Timestamp = requestedTimestamp }]
        };

        var handler = CreateHandler(db, moisture: moistureRepo);

        await handler.Handle(new RemoveMeasurementCommand
        {
            SensorUid = sensor.Uid,
            Timestamp = requestedTimestamp
        }, CancellationToken.None);

        Assert.Equal(requestedTimestamp, moistureRepo.LastDeleteFrom);
        Assert.Equal(requestedTimestamp, moistureRepo.LastDeleteTill);
    }

    [Fact]
    public async Task Handle_ThermometerSensor_SingleMeasurement_DeletesMeasurement()
    {
        await using var db = TestDbContext.Create();
        var sensor = TestEntityFactory.CreateSensor(SensorType.Thermometer, devEui: "RM_THM_01");
        db.Context.Sensors.Add(sensor);
        await db.Context.SaveChangesAsync();

        var requestedTimestamp = DateTime.UtcNow;
        var thermometerRepo = new FakeMeasurementThermometerRepository
        {
            GetResult = [new MeasurementThermometer { DevEui = "RM_THM_01", Timestamp = requestedTimestamp }]
        };

        var handler = CreateHandler(db, thermometer: thermometerRepo);

        await handler.Handle(new RemoveMeasurementCommand
        {
            SensorUid = sensor.Uid,
            Timestamp = requestedTimestamp
        }, CancellationToken.None);

        Assert.Equal(requestedTimestamp, thermometerRepo.LastDeleteFrom);
        Assert.Equal(requestedTimestamp, thermometerRepo.LastDeleteTill);
    }

    [Fact]
    public async Task Handle_UnsupportedSensorType_ThrowsInvalidOperationException()
    {
        await using var db = TestDbContext.Create();
        var sensor = TestEntityFactory.CreateSensor((SensorType)999, devEui: "RM_BAD_01");
        db.Context.Sensors.Add(sensor);
        await db.Context.SaveChangesAsync();

        var handler = CreateHandler(db);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new RemoveMeasurementCommand
            {
                SensorUid = sensor.Uid,
                Timestamp = DateTime.UtcNow
            }, CancellationToken.None));

        Assert.Contains("Unsupported sensor type", ex.Message);
    }
}

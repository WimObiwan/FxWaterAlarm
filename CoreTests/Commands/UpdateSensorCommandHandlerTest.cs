using Core.Commands;
using Core.Exceptions;
using Core.Util;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CoreTests.Commands;

public class UpdateSensorCommandHandlerTest
{
    [Fact]
    public async Task Handle_UpdatesExpectedIntervalSecs()
    {
        await using var db = TestDbContext.Create();
        var sensor = TestEntityFactory.CreateSensor(link: "uslink");
        db.Context.Sensors.Add(sensor);
        await db.Context.SaveChangesAsync();

        var handler = new UpdateSensorCommandHandler(db.Context);
        await handler.Handle(new UpdateSensorCommand
        {
            Uid = sensor.Uid,
            ExpectedIntervalSecs = new Optional<int?>(true, 3600)
        }, CancellationToken.None);

        var updated = await db.Context.Sensors.SingleAsync(s => s.Uid == sensor.Uid);
        Assert.Equal(3600, updated.ExpectedIntervalSecs);
    }

    [Fact]
    public async Task Handle_ClearsExpectedIntervalSecs()
    {
        await using var db = TestDbContext.Create();
        var sensor = TestEntityFactory.CreateSensor(link: "usclr");
        sensor.ExpectedIntervalSecs = 600;
        db.Context.Sensors.Add(sensor);
        await db.Context.SaveChangesAsync();

        var handler = new UpdateSensorCommandHandler(db.Context);
        await handler.Handle(new UpdateSensorCommand
        {
            Uid = sensor.Uid,
            ExpectedIntervalSecs = new Optional<int?>(true, null)
        }, CancellationToken.None);

        var updated = await db.Context.Sensors.SingleAsync(s => s.Uid == sensor.Uid);
        Assert.Null(updated.ExpectedIntervalSecs);
    }

    [Fact]
    public async Task Handle_UnspecifiedFieldsUnchanged()
    {
        await using var db = TestDbContext.Create();
        var sensor = TestEntityFactory.CreateSensor(link: "uskeep");
        sensor.ExpectedIntervalSecs = 900;
        db.Context.Sensors.Add(sensor);
        await db.Context.SaveChangesAsync();

        var handler = new UpdateSensorCommandHandler(db.Context);
        await handler.Handle(new UpdateSensorCommand
        {
            Uid = sensor.Uid
            // ExpectedIntervalSecs not specified
        }, CancellationToken.None);

        var updated = await db.Context.Sensors.SingleAsync(s => s.Uid == sensor.Uid);
        Assert.Equal(900, updated.ExpectedIntervalSecs);
    }

    [Fact]
    public async Task Handle_SensorNotFound_ThrowsSensorNotFoundException()
    {
        await using var db = TestDbContext.Create();
        var handler = new UpdateSensorCommandHandler(db.Context);

        await Assert.ThrowsAsync<SensorNotFoundException>(() =>
            handler.Handle(new UpdateSensorCommand
            {
                Uid = Guid.NewGuid(),
                ExpectedIntervalSecs = new Optional<int?>(true, 100)
            }, CancellationToken.None));
    }
}

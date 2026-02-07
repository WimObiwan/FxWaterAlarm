using Core.Commands;
using Core.Entities;
using Core.Exceptions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CoreTests.Commands;

public class RemoveSensorCommandHandlerTest
{
    [Fact]
    public async Task Handle_RemovesSensorFromDatabase()
    {
        await using var db = TestDbContext.Create();
        var sensor = TestEntityFactory.CreateSensor(link: "removeme");
        db.Context.Sensors.Add(sensor);
        await db.Context.SaveChangesAsync();

        var handler = new RemoveSensorCommandHandler(db.Context);
        await handler.Handle(new RemoveSensorCommand
        {
            SensorUid = sensor.Uid
        }, CancellationToken.None);

        var remaining = await db.Context.Sensors.ToListAsync();
        Assert.Empty(remaining);
    }

    [Fact]
    public async Task Handle_SensorNotFound_ThrowsSensorNotFoundException()
    {
        await using var db = TestDbContext.Create();
        var handler = new RemoveSensorCommandHandler(db.Context);

        await Assert.ThrowsAsync<SensorNotFoundException>(() =>
            handler.Handle(new RemoveSensorCommand
            {
                SensorUid = Guid.NewGuid()
            }, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_SensorStillLinked_ThrowsSensorCouldNotBeRemovedException()
    {
        await using var db = TestDbContext.Create();
        var (_, sensor, _) = await TestEntityFactory.SeedAccountWithSensor(db.Context,
            email: "linked@test.com", accountLink: "linkedlink", sensorLink: "linkedsensor");

        var handler = new RemoveSensorCommandHandler(db.Context);

        await Assert.ThrowsAsync<SensorCouldNotBeRemovedException>(() =>
            handler.Handle(new RemoveSensorCommand
            {
                SensorUid = sensor.Uid
            }, CancellationToken.None));
    }
}

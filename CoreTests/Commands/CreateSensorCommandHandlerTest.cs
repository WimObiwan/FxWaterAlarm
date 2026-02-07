using Core.Commands;
using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CoreTests.Commands;

public class CreateSensorCommandHandlerTest
{
    [Fact]
    public async Task Handle_CreatesSensorInDatabase()
    {
        await using var db = TestDbContext.Create();
        var handler = new CreateSensorCommandHandler(db.Context);
        var uid = Guid.NewGuid();

        await handler.Handle(new CreateSensorCommand
        {
            Uid = uid,
            DevEui = "A81758FFFE001234",
            SensorType = SensorType.Level
        }, CancellationToken.None);

        var sensor = await db.Context.Sensors.SingleOrDefaultAsync(s => s.Uid == uid);
        Assert.NotNull(sensor);
        Assert.Equal("A81758FFFE001234", sensor!.DevEui);
        Assert.Equal(SensorType.Level, sensor.Type);
    }

    [Fact]
    public async Task Handle_DetectType_CreatesSensorWithCorrectType()
    {
        await using var db = TestDbContext.Create();
        var handler = new CreateSensorCommandHandler(db.Context);
        var uid = Guid.NewGuid();

        await handler.Handle(new CreateSensorCommand
        {
            Uid = uid,
            DevEui = "A81758FFFE005678",
            SensorType = SensorType.Detect
        }, CancellationToken.None);

        var sensor = await db.Context.Sensors.SingleAsync(s => s.Uid == uid);
        Assert.Equal(SensorType.Detect, sensor.Type);
    }

    [Fact]
    public async Task Handle_SetsCreateTimestamp()
    {
        await using var db = TestDbContext.Create();
        var handler = new CreateSensorCommandHandler(db.Context);
        var uid = Guid.NewGuid();
        var before = DateTime.UtcNow;

        await handler.Handle(new CreateSensorCommand
        {
            Uid = uid,
            DevEui = "A81758FFFE009999",
            SensorType = SensorType.Moisture
        }, CancellationToken.None);

        var sensor = await db.Context.Sensors.SingleAsync(s => s.Uid == uid);
        Assert.True(sensor.CreateTimestamp >= before);
        Assert.True(sensor.CreateTimestamp <= DateTime.UtcNow);
    }
}

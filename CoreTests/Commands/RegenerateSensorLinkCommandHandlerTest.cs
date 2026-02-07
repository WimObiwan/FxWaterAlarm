using Core.Commands;
using Core.Exceptions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CoreTests.Commands;

public class RegenerateSensorLinkCommandHandlerTest
{
    [Fact]
    public async Task Handle_WithExplicitLink_SetsLink()
    {
        await using var db = TestDbContext.Create();
        var sensor = TestEntityFactory.CreateSensor(link: "oldslink");
        db.Context.Sensors.Add(sensor);
        await db.Context.SaveChangesAsync();

        var handler = new RegenerateSensorLinkCommandHandler(db.Context);
        await handler.Handle(new RegenerateSensorLinkCommand
        {
            SensorUid = sensor.Uid,
            Link = "newslink456"
        }, CancellationToken.None);

        var updated = await db.Context.Sensors.SingleAsync(s => s.Uid == sensor.Uid);
        Assert.Equal("newslink456", updated.Link);
    }

    [Fact]
    public async Task Handle_WithoutLink_GeneratesRandomLink()
    {
        await using var db = TestDbContext.Create();
        var sensor = TestEntityFactory.CreateSensor(link: "origsl");
        db.Context.Sensors.Add(sensor);
        await db.Context.SaveChangesAsync();

        var handler = new RegenerateSensorLinkCommandHandler(db.Context);
        await handler.Handle(new RegenerateSensorLinkCommand
        {
            SensorUid = sensor.Uid
        }, CancellationToken.None);

        var updated = await db.Context.Sensors.SingleAsync(s => s.Uid == sensor.Uid);
        Assert.NotNull(updated.Link);
        Assert.NotEqual("origsl", updated.Link);
    }

    [Fact]
    public async Task Handle_SensorNotFound_ThrowsSensorNotFoundException()
    {
        await using var db = TestDbContext.Create();
        var handler = new RegenerateSensorLinkCommandHandler(db.Context);

        await Assert.ThrowsAsync<SensorNotFoundException>(() =>
            handler.Handle(new RegenerateSensorLinkCommand
            {
                SensorUid = Guid.NewGuid()
            }, CancellationToken.None));
    }
}

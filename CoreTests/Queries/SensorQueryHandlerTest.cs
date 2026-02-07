using Core.Queries;
using Xunit;

namespace CoreTests.Queries;

public class SensorQueryHandlerTest
{
    [Fact]
    public async Task Handle_MatchingUid_ReturnsSensor()
    {
        await using var db = TestDbContext.Create();
        var sensor = TestEntityFactory.CreateSensor(link: "sq1");
        db.Context.Sensors.Add(sensor);
        await db.Context.SaveChangesAsync();
        var handler = new SensorQueryHandler(db.Context);

        var result = await handler.Handle(
            new SensorQuery { Uid = sensor.Uid },
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(sensor.Uid, result!.Uid);
    }

    [Fact]
    public async Task Handle_NoMatch_ReturnsNull()
    {
        await using var db = TestDbContext.Create();
        var handler = new SensorQueryHandler(db.Context);

        var result = await handler.Handle(
            new SensorQuery { Uid = Guid.NewGuid() },
            CancellationToken.None);

        Assert.Null(result);
    }
}

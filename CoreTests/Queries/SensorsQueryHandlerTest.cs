using Core.Queries;
using Xunit;

namespace CoreTests.Queries;

public class SensorsQueryHandlerTest
{
    [Fact]
    public async Task Handle_ReturnsAllSensors()
    {
        await using var db = TestDbContext.Create();
        db.Context.Sensors.Add(TestEntityFactory.CreateSensor(link: "ss1"));
        db.Context.Sensors.Add(TestEntityFactory.CreateSensor(link: "ss2"));
        await db.Context.SaveChangesAsync();
        var handler = new SensorsQueryHandler(db.Context);

        var result = await handler.Handle(new SensorsQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task Handle_Empty_ReturnsEmptyList()
    {
        await using var db = TestDbContext.Create();
        var handler = new SensorsQueryHandler(db.Context);

        var result = await handler.Handle(new SensorsQuery(), CancellationToken.None);

        Assert.Empty(result);
    }
}

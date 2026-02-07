using Core.Queries;
using Xunit;

namespace CoreTests.Queries;

public class SensorByLinkQueryHandlerTest
{
    [Fact]
    public async Task Handle_ByLink_ReturnsSensor()
    {
        await using var db = TestDbContext.Create();
        var sensor = TestEntityFactory.CreateSensor(link: "sbl1");
        db.Context.Sensors.Add(sensor);
        await db.Context.SaveChangesAsync();
        var handler = new SensorByLinkQueryHandler(db.Context);

        var result = await handler.Handle(
            new SensorByLinkQuery { SensorLink = "sbl1" },
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(sensor.Uid, result!.Uid);
    }

    [Fact]
    public async Task Handle_ByDevEui_ReturnsSensor()
    {
        await using var db = TestDbContext.Create();
        var sensor = TestEntityFactory.CreateSensor(link: "sbl2", devEui: "deveui1");
        db.Context.Sensors.Add(sensor);
        await db.Context.SaveChangesAsync();
        var handler = new SensorByLinkQueryHandler(db.Context);

        var result = await handler.Handle(
            new SensorByLinkQuery { SensorLink = "deveui1" },
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(sensor.Uid, result!.Uid);
    }

    [Fact]
    public async Task Handle_NoMatch_ReturnsNull()
    {
        await using var db = TestDbContext.Create();
        var handler = new SensorByLinkQueryHandler(db.Context);

        var result = await handler.Handle(
            new SensorByLinkQuery { SensorLink = "missing" },
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_WithIncludeAccount_IncludesAccountSensors()
    {
        await using var db = TestDbContext.Create();
        await TestEntityFactory.SeedAccountWithSensor(db.Context, sensorLink: "sblwia");
        var handler = new SensorByLinkQueryHandler(db.Context);

        var result = await handler.Handle(
            new SensorByLinkQuery { SensorLink = "sblwia", IncludeAccount = true },
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result!.AccountSensors);
    }
}

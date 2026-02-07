using Core.Queries;
using Xunit;

namespace CoreTests.Queries;

public class AccountSensorByLinkQueryHandlerTest
{
    [Fact]
    public async Task Handle_BySensorLink_ReturnsAccountSensor()
    {
        await using var db = TestDbContext.Create();
        var (_, sensor, _) = await TestEntityFactory.SeedAccountWithSensor(db.Context,
            accountLink: "alink", sensorLink: "slink");
        var handler = new AccountSensorByLinkQueryHandler(db.Context);

        var result = await handler.Handle(
            new AccountSensorByLinkQuery { SensorLink = "slink" },
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(sensor.Uid, result!.Sensor.Uid);
    }

    [Fact]
    public async Task Handle_ByDevEui_ReturnsAccountSensor()
    {
        await using var db = TestDbContext.Create();
        var (_, sensor, _) = await TestEntityFactory.SeedAccountWithSensor(db.Context,
            sensorLink: "slink2", devEui: "mydevice");
        var handler = new AccountSensorByLinkQueryHandler(db.Context);

        var result = await handler.Handle(
            new AccountSensorByLinkQuery { SensorLink = "mydevice" },
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(sensor.Uid, result!.Sensor.Uid);
    }

    [Fact]
    public async Task Handle_WithAccountLink_FiltersCorrectly()
    {
        await using var db = TestDbContext.Create();
        await TestEntityFactory.SeedAccountWithSensor(db.Context,
            email: "a1@test.com", accountLink: "acc1", sensorLink: "shared");
        var handler = new AccountSensorByLinkQueryHandler(db.Context);

        var result = await handler.Handle(
            new AccountSensorByLinkQuery { SensorLink = "shared", AccountLink = "acc1" },
            CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Handle_DisabledSensor_ReturnsNull()
    {
        await using var db = TestDbContext.Create();
        await TestEntityFactory.SeedAccountWithSensor(db.Context, sensorLink: "dlink", disabled: true);
        var handler = new AccountSensorByLinkQueryHandler(db.Context);

        var result = await handler.Handle(
            new AccountSensorByLinkQuery { SensorLink = "dlink" },
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_NoMatch_ReturnsNull()
    {
        await using var db = TestDbContext.Create();
        var handler = new AccountSensorByLinkQueryHandler(db.Context);

        var result = await handler.Handle(
            new AccountSensorByLinkQuery { SensorLink = "nope" },
            CancellationToken.None);

        Assert.Null(result);
    }
}

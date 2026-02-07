using Core.Queries;
using Xunit;

namespace CoreTests.Queries;

public class AccountSensorByIdQueryHandlerTest
{
    [Fact]
    public async Task Handle_MatchingIds_ReturnsAccountSensor()
    {
        await using var db = TestDbContext.Create();
        var (account, sensor, _) = await TestEntityFactory.SeedAccountWithSensor(db.Context);
        var handler = new AccountSensorByIdQueryHandler(db.Context);

        var result = await handler.Handle(
            new AccountSensorByIdQuery { AccountUid = account.Uid, SensorUid = sensor.Uid },
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(account.Uid, result!.Account.Uid);
        Assert.Equal(sensor.Uid, result.Sensor.Uid);
    }

    [Fact]
    public async Task Handle_DisabledSensor_ReturnsNull()
    {
        await using var db = TestDbContext.Create();
        var (account, sensor, _) = await TestEntityFactory.SeedAccountWithSensor(db.Context, disabled: true);
        var handler = new AccountSensorByIdQueryHandler(db.Context);

        var result = await handler.Handle(
            new AccountSensorByIdQuery { AccountUid = account.Uid, SensorUid = sensor.Uid },
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_WrongSensorUid_ReturnsNull()
    {
        await using var db = TestDbContext.Create();
        var (account, _, _) = await TestEntityFactory.SeedAccountWithSensor(db.Context);
        var handler = new AccountSensorByIdQueryHandler(db.Context);

        var result = await handler.Handle(
            new AccountSensorByIdQuery { AccountUid = account.Uid, SensorUid = Guid.NewGuid() },
            CancellationToken.None);

        Assert.Null(result);
    }
}

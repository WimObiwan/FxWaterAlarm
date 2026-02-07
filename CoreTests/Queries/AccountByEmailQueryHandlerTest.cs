using Core.Queries;
using Xunit;

namespace CoreTests.Queries;

public class AccountByEmailQueryHandlerTest
{
    [Fact]
    public async Task Handle_MatchingEmail_ReturnsAccount()
    {
        await using var db = TestDbContext.Create();
        var (account, _, _) = await TestEntityFactory.SeedAccountWithSensor(db.Context, email: "user@test.com");
        var handler = new AccountByEmailQueryHandler(db.Context);

        var result = await handler.Handle(new AccountByEmailQuery { Email = "user@test.com" }, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(account.Uid, result!.Uid);
    }

    [Fact]
    public async Task Handle_NoMatch_ReturnsNull()
    {
        await using var db = TestDbContext.Create();
        var handler = new AccountByEmailQueryHandler(db.Context);

        var result = await handler.Handle(new AccountByEmailQuery { Email = "nonexistent@test.com" }, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_IncludesAccountSensorsAndSensor()
    {
        await using var db = TestDbContext.Create();
        var (account, sensor, _) = await TestEntityFactory.SeedAccountWithSensor(db.Context, email: "inc@test.com");
        var handler = new AccountByEmailQueryHandler(db.Context);

        var result = await handler.Handle(new AccountByEmailQuery { Email = "inc@test.com" }, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result!.AccountSensors);
        Assert.Equal(sensor.Uid, result.AccountSensors.First().Sensor.Uid);
    }
}

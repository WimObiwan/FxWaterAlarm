using Core.Entities;
using Core.Queries;
using Xunit;

namespace CoreTests.Queries;

public class AccountByLinkQueryHandlerTest
{
    [Fact]
    public async Task Handle_MatchingLink_ReturnsAccount()
    {
        await using var db = TestDbContext.Create();
        var (account, _, _) = await TestEntityFactory.SeedAccountWithSensor(db.Context, accountLink: "mylink");
        var handler = new AccountByLinkQueryHandler(db.Context);

        var result = await handler.Handle(new AccountByLinkQuery { Link = "mylink" }, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(account.Uid, result!.Uid);
    }

    [Fact]
    public async Task Handle_NoMatch_ReturnsNull()
    {
        await using var db = TestDbContext.Create();
        var handler = new AccountByLinkQueryHandler(db.Context);

        var result = await handler.Handle(new AccountByLinkQuery { Link = "nolink" }, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_ExcludesDisabledSensors()
    {
        await using var db = TestDbContext.Create();
        var ctx = db.Context;
        var account = TestEntityFactory.CreateAccount("dis@test.com", "dislink");
        var sensor1 = TestEntityFactory.CreateSensor(link: "s1");
        var sensor2 = TestEntityFactory.CreateSensor(link: "s2");
        ctx.Accounts.Add(account);
        ctx.Sensors.AddRange(sensor1, sensor2);
        await ctx.SaveChangesAsync();

        ctx.Set<AccountSensor>().Add(new AccountSensor
        {
            Account = account, Sensor = sensor1, CreateTimestamp = DateTime.UtcNow, Disabled = false, Order = 0
        });
        ctx.Set<AccountSensor>().Add(new AccountSensor
        {
            Account = account, Sensor = sensor2, CreateTimestamp = DateTime.UtcNow, Disabled = true, Order = 1
        });
        await ctx.SaveChangesAsync();

        // Use fresh context to avoid change tracking interference with filtered includes
        await using var queryCtx = db.CreateFreshContext();
        var handler = new AccountByLinkQueryHandler(queryCtx);
        var result = await handler.Handle(new AccountByLinkQuery { Link = "dislink" }, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result!.AccountSensors);
        Assert.Equal(sensor1.Uid, result.AccountSensors.First().Sensor.Uid);
    }
}

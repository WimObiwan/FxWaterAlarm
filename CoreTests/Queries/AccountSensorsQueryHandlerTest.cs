using Core.Entities;
using Core.Exceptions;
using Core.Queries;
using Xunit;

namespace CoreTests.Queries;

public class AccountSensorsQueryHandlerTest
{
    [Fact]
    public async Task Handle_WithAccountUid_ReturnsAccountSensors()
    {
        await using var db = TestDbContext.Create();
        var (account, _, _) = await TestEntityFactory.SeedAccountWithSensor(db.Context);
        var handler = new AccountSensorsQueryHandler(db.Context);

        var result = await handler.Handle(
            new AccountSensorsQuery { AccountUid = account.Uid },
            CancellationToken.None);

        Assert.Single(result);
    }

    [Fact]
    public async Task Handle_AccountNotFound_ThrowsAccountNotFoundException()
    {
        await using var db = TestDbContext.Create();
        var handler = new AccountSensorsQueryHandler(db.Context);

        await Assert.ThrowsAsync<AccountNotFoundException>(() =>
            handler.Handle(
                new AccountSensorsQuery { AccountUid = Guid.NewGuid() },
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ExcludesDisabledByDefault()
    {
        await using var db = TestDbContext.Create();
        var ctx = db.Context;
        var account = TestEntityFactory.CreateAccount("ex@t.com", "exlink");
        var sensor1 = TestEntityFactory.CreateSensor(link: "es1");
        var sensor2 = TestEntityFactory.CreateSensor(link: "es2");
        ctx.Accounts.Add(account);
        ctx.Sensors.AddRange(sensor1, sensor2);
        await ctx.SaveChangesAsync();

        ctx.Set<AccountSensor>().Add(new AccountSensor
        {
            Account = account, Sensor = sensor1, CreateTimestamp = DateTime.UtcNow, Disabled = false
        });
        ctx.Set<AccountSensor>().Add(new AccountSensor
        {
            Account = account, Sensor = sensor2, CreateTimestamp = DateTime.UtcNow, Disabled = true
        });
        await ctx.SaveChangesAsync();

        // Use fresh context to avoid change tracking interference with filtered includes
        await using var queryCtx = db.CreateFreshContext();
        var handler = new AccountSensorsQueryHandler(queryCtx);
        var result = await handler.Handle(
            new AccountSensorsQuery { AccountUid = account.Uid },
            CancellationToken.None);

        Assert.Single(result);
    }

    [Fact]
    public async Task Handle_IncludeDisabled_ReturnsBoth()
    {
        await using var db = TestDbContext.Create();
        var ctx = db.Context;
        var account = TestEntityFactory.CreateAccount("id@t.com", "idlink");
        var sensor1 = TestEntityFactory.CreateSensor(link: "is1");
        var sensor2 = TestEntityFactory.CreateSensor(link: "is2");
        ctx.Accounts.Add(account);
        ctx.Sensors.AddRange(sensor1, sensor2);
        await ctx.SaveChangesAsync();

        ctx.Set<AccountSensor>().Add(new AccountSensor
        {
            Account = account, Sensor = sensor1, CreateTimestamp = DateTime.UtcNow, Disabled = false
        });
        ctx.Set<AccountSensor>().Add(new AccountSensor
        {
            Account = account, Sensor = sensor2, CreateTimestamp = DateTime.UtcNow, Disabled = true
        });
        await ctx.SaveChangesAsync();

        var handler = new AccountSensorsQueryHandler(ctx);
        var result = await handler.Handle(
            new AccountSensorsQuery { AccountUid = account.Uid, IncludeDisabled = true },
            CancellationToken.None);

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task Handle_NoAccountUid_ReturnsAllAccountSensors()
    {
        await using var db = TestDbContext.Create();
        var ctx = db.Context;
        await TestEntityFactory.SeedAccountWithSensor(ctx, email: "a1@t.com", accountLink: "al1", sensorLink: "sl1");
        await TestEntityFactory.SeedAccountWithSensor(ctx, email: "a2@t.com", accountLink: "al2", sensorLink: "sl2");
        var handler = new AccountSensorsQueryHandler(ctx);

        var result = await handler.Handle(new AccountSensorsQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count());
    }
}

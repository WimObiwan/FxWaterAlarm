using Core.Commands;
using Core.Entities;
using Core.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CoreTests.Commands;

public class ResetAccountSensorOrderCommandHandlerTest
{
    [Fact]
    public async Task Handle_SingleAccount_ResetsOrder()
    {
        await using var db = TestDbContext.Create();
        var account = TestEntityFactory.CreateAccount("rasorder@test.com", "rasorderlink");
        var sensor1 = TestEntityFactory.CreateSensor(link: "rsos1");
        var sensor2 = TestEntityFactory.CreateSensor(link: "rsos2");
        db.Context.Accounts.Add(account);
        db.Context.Sensors.AddRange(sensor1, sensor2);
        await db.Context.SaveChangesAsync();

        db.Context.Set<AccountSensor>().Add(new AccountSensor
        {
            Account = account, Sensor = sensor1, CreateTimestamp = DateTime.UtcNow, Order = 5
        });
        db.Context.Set<AccountSensor>().Add(new AccountSensor
        {
            Account = account, Sensor = sensor2, CreateTimestamp = DateTime.UtcNow, Order = 10
        });
        await db.Context.SaveChangesAsync();

        var handler = new ResetAccountSensorOrderCommandHandler(db.Context,
            NullLogger<CheckAllAccountSensorAlarmsCommandHandler>.Instance);

        await handler.Handle(new ResetAccountSensorOrderCommand
        {
            AccountUid = account.Uid
        }, CancellationToken.None);

        await using var freshCtx = db.CreateFreshContext();
        var sensors = await freshCtx.Set<AccountSensor>().OrderBy(a => a.Order).ToListAsync();
        Assert.Equal(2, sensors.Count);
        Assert.Equal(0, sensors[0].Order);
        Assert.Equal(1, sensors[1].Order);
    }

    [Fact]
    public async Task Handle_AllAccounts_ResetsOrderForEach()
    {
        await using var db = TestDbContext.Create();
        var account1 = TestEntityFactory.CreateAccount("rasall1@test.com", "rasall1link");
        var account2 = TestEntityFactory.CreateAccount("rasall2@test.com", "rasall2link");
        var sensor1 = TestEntityFactory.CreateSensor(link: "rsoas1");
        var sensor2 = TestEntityFactory.CreateSensor(link: "rsoas2");
        db.Context.Accounts.AddRange(account1, account2);
        db.Context.Sensors.AddRange(sensor1, sensor2);
        await db.Context.SaveChangesAsync();

        db.Context.Set<AccountSensor>().Add(new AccountSensor
        {
            Account = account1, Sensor = sensor1, CreateTimestamp = DateTime.UtcNow, Order = 7
        });
        db.Context.Set<AccountSensor>().Add(new AccountSensor
        {
            Account = account2, Sensor = sensor2, CreateTimestamp = DateTime.UtcNow, Order = 3
        });
        await db.Context.SaveChangesAsync();

        var handler = new ResetAccountSensorOrderCommandHandler(db.Context,
            NullLogger<CheckAllAccountSensorAlarmsCommandHandler>.Instance);

        await handler.Handle(new ResetAccountSensorOrderCommand
        {
            AccountUid = null
        }, CancellationToken.None);

        await using var freshCtx = db.CreateFreshContext();
        var sensors = await freshCtx.Set<AccountSensor>().ToListAsync();
        Assert.All(sensors, s => Assert.Equal(0, s.Order));
    }

    [Fact]
    public async Task Handle_AlreadyCorrectOrder_NoChange()
    {
        await using var db = TestDbContext.Create();
        var account = TestEntityFactory.CreateAccount("rasnoch@test.com", "rasnochlink");
        var sensor1 = TestEntityFactory.CreateSensor(link: "rsonc1");
        db.Context.Accounts.Add(account);
        db.Context.Sensors.Add(sensor1);
        await db.Context.SaveChangesAsync();

        db.Context.Set<AccountSensor>().Add(new AccountSensor
        {
            Account = account, Sensor = sensor1, CreateTimestamp = DateTime.UtcNow, Order = 0
        });
        await db.Context.SaveChangesAsync();

        var handler = new ResetAccountSensorOrderCommandHandler(db.Context,
            NullLogger<CheckAllAccountSensorAlarmsCommandHandler>.Instance);

        // Should succeed without saving (order already correct)
        await handler.Handle(new ResetAccountSensorOrderCommand
        {
            AccountUid = account.Uid
        }, CancellationToken.None);

        await using var freshCtx = db.CreateFreshContext();
        var sensor = await freshCtx.Set<AccountSensor>().SingleAsync();
        Assert.Equal(0, sensor.Order);
    }

    [Fact]
    public async Task Handle_AccountNotFound_ThrowsAccountNotFoundException()
    {
        await using var db = TestDbContext.Create();
        var handler = new ResetAccountSensorOrderCommandHandler(db.Context,
            NullLogger<CheckAllAccountSensorAlarmsCommandHandler>.Instance);

        await Assert.ThrowsAsync<AccountNotFoundException>(() =>
            handler.Handle(new ResetAccountSensorOrderCommand
            {
                AccountUid = Guid.NewGuid()
            }, CancellationToken.None));
    }
}

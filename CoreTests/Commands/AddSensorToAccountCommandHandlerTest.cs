using Core.Commands;
using Core.Entities;
using Core.Exceptions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CoreTests.Commands;

public class AddSensorToAccountCommandHandlerTest
{
    [Fact]
    public async Task Handle_AddsSensorToAccount()
    {
        await using var db = TestDbContext.Create();
        var account = TestEntityFactory.CreateAccount("add@test.com", "addlink");
        var sensor = TestEntityFactory.CreateSensor(link: "addsensor");
        db.Context.Accounts.Add(account);
        db.Context.Sensors.Add(sensor);
        await db.Context.SaveChangesAsync();

        var handler = new AddSensorToAccountCommandHandler(db.Context);
        await handler.Handle(new AddSensorToAccountCommand
        {
            AccountUid = account.Uid,
            SensorUid = sensor.Uid
        }, CancellationToken.None);

        var result = await db.Context.Set<AccountSensor>()
            .Include(a => a.Account)
            .Include(a => a.Sensor)
            .FirstOrDefaultAsync();
        Assert.NotNull(result);
        Assert.Equal(account.Uid, result!.Account.Uid);
        Assert.Equal(sensor.Uid, result.Sensor.Uid);
    }

    [Fact]
    public async Task Handle_AccountNotFound_ThrowsAccountNotFoundException()
    {
        await using var db = TestDbContext.Create();
        var sensor = TestEntityFactory.CreateSensor(link: "orphan");
        db.Context.Sensors.Add(sensor);
        await db.Context.SaveChangesAsync();

        var handler = new AddSensorToAccountCommandHandler(db.Context);

        await Assert.ThrowsAsync<AccountNotFoundException>(() =>
            handler.Handle(new AddSensorToAccountCommand
            {
                AccountUid = Guid.NewGuid(),
                SensorUid = sensor.Uid
            }, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_SensorNotFound_ThrowsSensorNotFoundException()
    {
        await using var db = TestDbContext.Create();
        var account = TestEntityFactory.CreateAccount("nosensor@test.com", "nslink");
        db.Context.Accounts.Add(account);
        await db.Context.SaveChangesAsync();

        var handler = new AddSensorToAccountCommandHandler(db.Context);

        await Assert.ThrowsAsync<SensorNotFoundException>(() =>
            handler.Handle(new AddSensorToAccountCommand
            {
                AccountUid = account.Uid,
                SensorUid = Guid.NewGuid()
            }, CancellationToken.None));
    }
}

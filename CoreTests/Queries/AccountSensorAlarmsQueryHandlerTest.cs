using Core.Entities;
using Core.Exceptions;
using Core.Queries;
using Xunit;

namespace CoreTests.Queries;

public class AccountSensorAlarmsQueryHandlerTest
{
    [Fact]
    public async Task Handle_ExistingAccountSensor_ReturnsAlarms()
    {
        await using var db = TestDbContext.Create();
        var ctx = db.Context;
        var (account, sensor, accountSensor) = await TestEntityFactory.SeedAccountWithSensor(ctx);

        var alarm = new AccountSensorAlarm
        {
            Uid = Guid.NewGuid(),
            AlarmType = AccountSensorAlarmType.Data,
            AccountSensor = accountSensor
        };
        ctx.Set<AccountSensorAlarm>().Add(alarm);
        await ctx.SaveChangesAsync();

        var handler = new AccountSensorAlarmsQueryHandler(ctx);
        var result = await handler.Handle(
            new AccountSensorAlarmsQuery { AccountUid = account.Uid, SensorUid = sensor.Uid },
            CancellationToken.None);

        Assert.Single(result);
    }

    [Fact]
    public async Task Handle_NotFound_ThrowsAccountSensorNotFoundException()
    {
        await using var db = TestDbContext.Create();
        var ctx = db.Context;
        var account = TestEntityFactory.CreateAccount("a@t.com");
        ctx.Accounts.Add(account);
        await ctx.SaveChangesAsync();

        var handler = new AccountSensorAlarmsQueryHandler(ctx);

        await Assert.ThrowsAsync<AccountSensorNotFoundException>(() =>
            handler.Handle(
                new AccountSensorAlarmsQuery { AccountUid = account.Uid, SensorUid = Guid.NewGuid() },
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DisabledSensor_ThrowsAccountSensorDisabledException()
    {
        await using var db = TestDbContext.Create();
        var (account, sensor, _) = await TestEntityFactory.SeedAccountWithSensor(db.Context, disabled: true);

        var handler = new AccountSensorAlarmsQueryHandler(db.Context);

        await Assert.ThrowsAsync<AccountSensorDisabledException>(() =>
            handler.Handle(
                new AccountSensorAlarmsQuery { AccountUid = account.Uid, SensorUid = sensor.Uid },
                CancellationToken.None));
    }
}

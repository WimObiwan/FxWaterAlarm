using Core.Commands;
using Core.Entities;
using Core.Exceptions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CoreTests.Commands;

public class RemoveAlarmFromAccountSensorCommandHandlerTest
{
    private static async Task<(Account account, Sensor sensor, AccountSensorAlarm alarm)> SeedWithAlarm(
        TestDbContext db, bool disabled = false)
    {
        var (account, sensor, accountSensor) = await TestEntityFactory.SeedAccountWithSensor(db.Context,
            email: $"rmalarm-{Guid.NewGuid():N}@test.com",
            accountLink: $"ral-{Guid.NewGuid():N}",
            sensorLink: $"rals-{Guid.NewGuid():N}",
            disabled: disabled);

        // Load the Alarms collection so the backing field is populated by EF
        await db.Context.Entry(accountSensor).Collection(a => a.Alarms).LoadAsync();

        var alarm = new AccountSensorAlarm
        {
            Uid = Guid.NewGuid(),
            AlarmType = AccountSensorAlarmType.Data,
            AlarmThreshold = 24.5
        };
        accountSensor.AddAlarm(alarm);
        await db.Context.SaveChangesAsync();

        return (account, sensor, alarm);
    }

    [Fact]
    public async Task Handle_RemovesAlarm()
    {
        await using var db = TestDbContext.Create();
        var (account, sensor, alarm) = await SeedWithAlarm(db);

        var handler = new RemoveAlarmFromAccountSensorCommandHandler(db.Context);
        await handler.Handle(new RemoveAlarmFromAccountSensorCommand
        {
            AccountUid = account.Uid,
            SensorUid = sensor.Uid,
            AlarmUid = alarm.Uid
        }, CancellationToken.None);

        await using var freshCtx = db.CreateFreshContext();
        var alarms = await freshCtx.Set<AccountSensorAlarm>().ToListAsync();
        Assert.Empty(alarms);
    }

    [Fact]
    public async Task Handle_AccountSensorNotFound_ThrowsAccountSensorNotFoundException()
    {
        await using var db = TestDbContext.Create();
        var handler = new RemoveAlarmFromAccountSensorCommandHandler(db.Context);

        await Assert.ThrowsAsync<AccountSensorNotFoundException>(() =>
            handler.Handle(new RemoveAlarmFromAccountSensorCommand
            {
                AccountUid = Guid.NewGuid(),
                SensorUid = Guid.NewGuid(),
                AlarmUid = Guid.NewGuid()
            }, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AlarmNotFound_ThrowsAccountSensorAlarmNotFoundException()
    {
        await using var db = TestDbContext.Create();
        var (account, sensor, _) = await SeedWithAlarm(db);

        var handler = new RemoveAlarmFromAccountSensorCommandHandler(db.Context);

        await Assert.ThrowsAsync<AccountSensorAlarmNotFoundException>(() =>
            handler.Handle(new RemoveAlarmFromAccountSensorCommand
            {
                AccountUid = account.Uid,
                SensorUid = sensor.Uid,
                AlarmUid = Guid.NewGuid() // non-existent alarm
            }, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DisabledAccountSensor_ThrowsAccountSensorDisabledException()
    {
        await using var db = TestDbContext.Create();
        var (account, sensor, alarm) = await SeedWithAlarm(db, disabled: true);

        var handler = new RemoveAlarmFromAccountSensorCommandHandler(db.Context);

        await Assert.ThrowsAsync<AccountSensorDisabledException>(() =>
            handler.Handle(new RemoveAlarmFromAccountSensorCommand
            {
                AccountUid = account.Uid,
                SensorUid = sensor.Uid,
                AlarmUid = alarm.Uid
            }, CancellationToken.None));
    }
}

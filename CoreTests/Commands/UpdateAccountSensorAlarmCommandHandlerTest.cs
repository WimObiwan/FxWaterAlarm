using Core.Commands;
using Core.Entities;
using Core.Exceptions;
using Core.Util;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CoreTests.Commands;

public class UpdateAccountSensorAlarmCommandHandlerTest
{
    private static async Task<(Account account, Sensor sensor, AccountSensorAlarm alarm)> SeedWithAlarm(
        TestDbContext db, bool disabled = false)
    {
        var (account, sensor, accountSensor) = await TestEntityFactory.SeedAccountWithSensor(db.Context,
            email: $"ualarm-{Guid.NewGuid():N}@test.com",
            accountLink: $"ual-{Guid.NewGuid():N}",
            sensorLink: $"uals-{Guid.NewGuid():N}",
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
    public async Task Handle_UpdatesAlarmType()
    {
        await using var db = TestDbContext.Create();
        var (account, sensor, alarm) = await SeedWithAlarm(db);

        var handler = new UpdateAccountSensorAlarmCommandHandler(db.Context);
        await handler.Handle(new UpdateAccountSensorAlarmCommand
        {
            AccountUid = account.Uid,
            SensorUid = sensor.Uid,
            AlarmUid = alarm.Uid,
            AlarmType = new Optional<AccountSensorAlarmType>(true, AccountSensorAlarmType.Battery),
            AlarmThreshold = default
        }, CancellationToken.None);

        await using var freshCtx = db.CreateFreshContext();
        var updated = await freshCtx.Set<AccountSensorAlarm>().SingleAsync(a => a.Uid == alarm.Uid);
        Assert.Equal(AccountSensorAlarmType.Battery, updated.AlarmType);
        Assert.Equal(24.5, updated.AlarmThreshold); // unchanged
    }

    [Fact]
    public async Task Handle_UpdatesAlarmThreshold()
    {
        await using var db = TestDbContext.Create();
        var (account, sensor, alarm) = await SeedWithAlarm(db);

        var handler = new UpdateAccountSensorAlarmCommandHandler(db.Context);
        await handler.Handle(new UpdateAccountSensorAlarmCommand
        {
            AccountUid = account.Uid,
            SensorUid = sensor.Uid,
            AlarmUid = alarm.Uid,
            AlarmType = default,
            AlarmThreshold = new Optional<double?>(true, 50.0)
        }, CancellationToken.None);

        await using var freshCtx = db.CreateFreshContext();
        var updated = await freshCtx.Set<AccountSensorAlarm>().SingleAsync(a => a.Uid == alarm.Uid);
        Assert.Equal(AccountSensorAlarmType.Data, updated.AlarmType); // unchanged
        Assert.Equal(50.0, updated.AlarmThreshold);
    }

    [Fact]
    public async Task Handle_UpdatesBothFields()
    {
        await using var db = TestDbContext.Create();
        var (account, sensor, alarm) = await SeedWithAlarm(db);

        var handler = new UpdateAccountSensorAlarmCommandHandler(db.Context);
        await handler.Handle(new UpdateAccountSensorAlarmCommand
        {
            AccountUid = account.Uid,
            SensorUid = sensor.Uid,
            AlarmUid = alarm.Uid,
            AlarmType = new Optional<AccountSensorAlarmType>(true, AccountSensorAlarmType.PercentageLow),
            AlarmThreshold = new Optional<double?>(true, 15.0)
        }, CancellationToken.None);

        await using var freshCtx = db.CreateFreshContext();
        var updated = await freshCtx.Set<AccountSensorAlarm>().SingleAsync(a => a.Uid == alarm.Uid);
        Assert.Equal(AccountSensorAlarmType.PercentageLow, updated.AlarmType);
        Assert.Equal(15.0, updated.AlarmThreshold);
    }

    [Fact]
    public async Task Handle_AccountSensorNotFound_ThrowsAccountSensorNotFoundException()
    {
        await using var db = TestDbContext.Create();
        var handler = new UpdateAccountSensorAlarmCommandHandler(db.Context);

        await Assert.ThrowsAsync<AccountSensorNotFoundException>(() =>
            handler.Handle(new UpdateAccountSensorAlarmCommand
            {
                AccountUid = Guid.NewGuid(),
                SensorUid = Guid.NewGuid(),
                AlarmUid = Guid.NewGuid(),
                AlarmType = default,
                AlarmThreshold = default
            }, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AlarmNotFound_ThrowsAccountSensorAlarmNotFoundException()
    {
        await using var db = TestDbContext.Create();
        var (account, sensor, _) = await SeedWithAlarm(db);

        var handler = new UpdateAccountSensorAlarmCommandHandler(db.Context);

        await Assert.ThrowsAsync<AccountSensorAlarmNotFoundException>(() =>
            handler.Handle(new UpdateAccountSensorAlarmCommand
            {
                AccountUid = account.Uid,
                SensorUid = sensor.Uid,
                AlarmUid = Guid.NewGuid(),
                AlarmType = new Optional<AccountSensorAlarmType>(true, AccountSensorAlarmType.Battery),
                AlarmThreshold = default
            }, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DisabledAccountSensor_ThrowsAccountSensorDisabledException()
    {
        await using var db = TestDbContext.Create();
        var (account, sensor, alarm) = await SeedWithAlarm(db, disabled: true);

        var handler = new UpdateAccountSensorAlarmCommandHandler(db.Context);

        await Assert.ThrowsAsync<AccountSensorDisabledException>(() =>
            handler.Handle(new UpdateAccountSensorAlarmCommand
            {
                AccountUid = account.Uid,
                SensorUid = sensor.Uid,
                AlarmUid = alarm.Uid,
                AlarmType = new Optional<AccountSensorAlarmType>(true, AccountSensorAlarmType.Battery),
                AlarmThreshold = default
            }, CancellationToken.None));
    }
}

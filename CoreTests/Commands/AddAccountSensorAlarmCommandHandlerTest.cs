using Core.Commands;
using Core.Entities;
using Core.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CoreTests.Commands;

public class AddAccountSensorAlarmCommandHandlerTest
{
    [Fact]
    public async Task Handle_AddsAlarm()
    {
        await using var db = TestDbContext.Create();
        var (account, sensor, _) = await TestEntityFactory.SeedAccountWithSensor(db.Context,
            email: "alarm@test.com", accountLink: "alarmlink", sensorLink: "alarmsensor");

        var alarmUid = Guid.NewGuid();
        var handler = new AddAccountSensorAlarmCommandHandler(db.Context,
            NullLogger<AddAccountSensorAlarmCommandHandler>.Instance);

        await handler.Handle(new AddAccountSensorAlarmCommand
        {
            AccountId = account.Uid,
            SensorId = sensor.Uid,
            AlarmId = alarmUid,
            AlarmType = AccountSensorAlarmType.Data,
            AlarmThreshold = 24.5
        }, CancellationToken.None);

        await using var freshCtx = db.CreateFreshContext();
        var alarms = await freshCtx.Set<AccountSensorAlarm>().ToListAsync();
        Assert.Single(alarms);
        Assert.Equal(alarmUid, alarms[0].Uid);
        Assert.Equal(AccountSensorAlarmType.Data, alarms[0].AlarmType);
        Assert.Equal(24.5, alarms[0].AlarmThreshold);
    }

    [Fact]
    public async Task Handle_AccountSensorNotFound_ThrowsAccountSensorNotFoundException()
    {
        await using var db = TestDbContext.Create();
        var handler = new AddAccountSensorAlarmCommandHandler(db.Context,
            NullLogger<AddAccountSensorAlarmCommandHandler>.Instance);

        await Assert.ThrowsAsync<AccountSensorNotFoundException>(() =>
            handler.Handle(new AddAccountSensorAlarmCommand
            {
                AccountId = Guid.NewGuid(),
                SensorId = Guid.NewGuid(),
                AlarmId = Guid.NewGuid(),
                AlarmType = AccountSensorAlarmType.Data,
                AlarmThreshold = 10.0
            }, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DisabledAccountSensor_ThrowsAccountSensorDisabledException()
    {
        await using var db = TestDbContext.Create();
        var (account, sensor, _) = await TestEntityFactory.SeedAccountWithSensor(db.Context,
            email: "disabled@test.com", accountLink: "dislink", sensorLink: "dissensor", disabled: true);

        var handler = new AddAccountSensorAlarmCommandHandler(db.Context,
            NullLogger<AddAccountSensorAlarmCommandHandler>.Instance);

        await Assert.ThrowsAsync<AccountSensorDisabledException>(() =>
            handler.Handle(new AddAccountSensorAlarmCommand
            {
                AccountId = account.Uid,
                SensorId = sensor.Uid,
                AlarmId = Guid.NewGuid(),
                AlarmType = AccountSensorAlarmType.Battery,
                AlarmThreshold = 20.0
            }, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AddsAlarmWithNullThreshold()
    {
        await using var db = TestDbContext.Create();
        var (account, sensor, _) = await TestEntityFactory.SeedAccountWithSensor(db.Context,
            email: "nullthr@test.com", accountLink: "ntlink", sensorLink: "ntsensor");

        var handler = new AddAccountSensorAlarmCommandHandler(db.Context,
            NullLogger<AddAccountSensorAlarmCommandHandler>.Instance);

        await handler.Handle(new AddAccountSensorAlarmCommand
        {
            AccountId = account.Uid,
            SensorId = sensor.Uid,
            AlarmId = Guid.NewGuid(),
            AlarmType = AccountSensorAlarmType.DetectOn,
            AlarmThreshold = null
        }, CancellationToken.None);

        await using var freshCtx = db.CreateFreshContext();
        var alarm = await freshCtx.Set<AccountSensorAlarm>().SingleAsync();
        Assert.Equal(AccountSensorAlarmType.DetectOn, alarm.AlarmType);
        Assert.Null(alarm.AlarmThreshold);
    }
}

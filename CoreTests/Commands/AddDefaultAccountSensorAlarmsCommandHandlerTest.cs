using Core.Commands;
using Core.Entities;
using Core.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CoreTests.Commands;

public class AddDefaultAccountSensorAlarmsCommandHandlerTest
{
    [Fact]
    public async Task Handle_LevelSensor_CreatesDataAndPercentageLowAlarms()
    {
        await using var db = TestDbContext.Create();
        var (account, sensor, _) = await TestEntityFactory.SeedAccountWithSensor(db.Context,
            email: "deflev@test.com", accountLink: "deflevlink", sensorLink: "deflevsensor",
            sensorType: SensorType.Level);

        var handler = new AddDefaultAccountSensorAlarmsCommandHandler(db.Context,
            NullLogger<AddDefaultAccountSensorAlarmsCommandHandler>.Instance);

        await handler.Handle(new AddDefaultAccountSensorAlarmsCommand
        {
            AccountId = account.Uid,
            SensorId = sensor.Uid
        }, CancellationToken.None);

        await using var freshCtx = db.CreateFreshContext();
        var alarms = await freshCtx.Set<AccountSensorAlarm>().ToListAsync();
        Assert.Equal(2, alarms.Count);
        Assert.Contains(alarms, a => a.AlarmType == AccountSensorAlarmType.Data && a.AlarmThreshold == 24.5);
        Assert.Contains(alarms, a => a.AlarmType == AccountSensorAlarmType.PercentageLow && a.AlarmThreshold == 25.0);
    }

    [Fact]
    public async Task Handle_DetectSensor_CreatesDataAndDetectOnAlarms()
    {
        await using var db = TestDbContext.Create();
        var (account, sensor, _) = await TestEntityFactory.SeedAccountWithSensor(db.Context,
            email: "defdet@test.com", accountLink: "defdetlink", sensorLink: "defdetsensor",
            sensorType: SensorType.Detect);

        var handler = new AddDefaultAccountSensorAlarmsCommandHandler(db.Context,
            NullLogger<AddDefaultAccountSensorAlarmsCommandHandler>.Instance);

        await handler.Handle(new AddDefaultAccountSensorAlarmsCommand
        {
            AccountId = account.Uid,
            SensorId = sensor.Uid
        }, CancellationToken.None);

        await using var freshCtx = db.CreateFreshContext();
        var alarms = await freshCtx.Set<AccountSensorAlarm>().ToListAsync();
        Assert.Equal(2, alarms.Count);
        Assert.Contains(alarms, a => a.AlarmType == AccountSensorAlarmType.Data);
        Assert.Contains(alarms, a => a.AlarmType == AccountSensorAlarmType.DetectOn);
    }

    [Fact]
    public async Task Handle_MoistureSensor_CreatesOnlyDataAlarm()
    {
        await using var db = TestDbContext.Create();
        var (account, sensor, _) = await TestEntityFactory.SeedAccountWithSensor(db.Context,
            email: "defmoi@test.com", accountLink: "defmoilink", sensorLink: "defmoisensor",
            sensorType: SensorType.Moisture);

        var handler = new AddDefaultAccountSensorAlarmsCommandHandler(db.Context,
            NullLogger<AddDefaultAccountSensorAlarmsCommandHandler>.Instance);

        await handler.Handle(new AddDefaultAccountSensorAlarmsCommand
        {
            AccountId = account.Uid,
            SensorId = sensor.Uid
        }, CancellationToken.None);

        await using var freshCtx = db.CreateFreshContext();
        var alarms = await freshCtx.Set<AccountSensorAlarm>().ToListAsync();
        Assert.Single(alarms);
        Assert.Equal(AccountSensorAlarmType.Data, alarms[0].AlarmType);
    }

    [Fact]
    public async Task Handle_ThermometerSensor_CreatesOnlyDataAlarm()
    {
        await using var db = TestDbContext.Create();
        var (account, sensor, _) = await TestEntityFactory.SeedAccountWithSensor(db.Context,
            email: "defthm@test.com", accountLink: "defthmlink", sensorLink: "defthmsensor",
            sensorType: SensorType.Thermometer);

        var handler = new AddDefaultAccountSensorAlarmsCommandHandler(db.Context,
            NullLogger<AddDefaultAccountSensorAlarmsCommandHandler>.Instance);

        await handler.Handle(new AddDefaultAccountSensorAlarmsCommand
        {
            AccountId = account.Uid,
            SensorId = sensor.Uid
        }, CancellationToken.None);

        await using var freshCtx = db.CreateFreshContext();
        var alarms = await freshCtx.Set<AccountSensorAlarm>().ToListAsync();
        Assert.Single(alarms);
        Assert.Equal(AccountSensorAlarmType.Data, alarms[0].AlarmType);
    }

    [Fact]
    public async Task Handle_ExistingAlarms_SkipsCreation()
    {
        await using var db = TestDbContext.Create();
        var (account, sensor, accountSensor) = await TestEntityFactory.SeedAccountWithSensor(db.Context,
            email: "defskip@test.com", accountLink: "defskiplink", sensorLink: "defskipsensor");

        // Load the Alarms collection so the backing field is populated by EF
        await db.Context.Entry(accountSensor).Collection(a => a.Alarms).LoadAsync();

        // Pre-add an alarm
        accountSensor.AddAlarm(new AccountSensorAlarm
        {
            Uid = Guid.NewGuid(),
            AlarmType = AccountSensorAlarmType.Battery,
            AlarmThreshold = 10.0
        });
        await db.Context.SaveChangesAsync();

        var handler = new AddDefaultAccountSensorAlarmsCommandHandler(db.Context,
            NullLogger<AddDefaultAccountSensorAlarmsCommandHandler>.Instance);

        await handler.Handle(new AddDefaultAccountSensorAlarmsCommand
        {
            AccountId = account.Uid,
            SensorId = sensor.Uid
        }, CancellationToken.None);

        // Should still only have original alarm, nothing added
        await using var freshCtx = db.CreateFreshContext();
        var alarms = await freshCtx.Set<AccountSensorAlarm>().ToListAsync();
        Assert.Single(alarms);
        Assert.Equal(AccountSensorAlarmType.Battery, alarms[0].AlarmType);
    }

    [Fact]
    public async Task Handle_DisabledAccountSensor_ThrowsAccountSensorDisabledException()
    {
        await using var db = TestDbContext.Create();
        var (account, sensor, _) = await TestEntityFactory.SeedAccountWithSensor(db.Context,
            email: "defdis@test.com", accountLink: "defdislink", sensorLink: "defdissensor",
            disabled: true);

        var handler = new AddDefaultAccountSensorAlarmsCommandHandler(db.Context,
            NullLogger<AddDefaultAccountSensorAlarmsCommandHandler>.Instance);

        await Assert.ThrowsAsync<AccountSensorDisabledException>(() =>
            handler.Handle(new AddDefaultAccountSensorAlarmsCommand
            {
                AccountId = account.Uid,
                SensorId = sensor.Uid
            }, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AccountSensorNotFound_ThrowsAccountSensorNotFoundException()
    {
        await using var db = TestDbContext.Create();
        var handler = new AddDefaultAccountSensorAlarmsCommandHandler(db.Context,
            NullLogger<AddDefaultAccountSensorAlarmsCommandHandler>.Instance);

        await Assert.ThrowsAsync<AccountSensorNotFoundException>(() =>
            handler.Handle(new AddDefaultAccountSensorAlarmsCommand
            {
                AccountId = Guid.NewGuid(),
                SensorId = Guid.NewGuid()
            }, CancellationToken.None));
    }
}

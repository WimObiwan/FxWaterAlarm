using Core.Commands;
using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CoreTests.Commands;

public class AddAllDefaultAccountSensorAlarmsCommandHandlerTest
{
    [Fact]
    public async Task Handle_CreatesDefaultAlarmsForAllAccountSensors()
    {
        await using var db = TestDbContext.Create();
        // Seed two account-sensor combos with different sensor types
        await TestEntityFactory.SeedAccountWithSensor(db.Context,
            email: "all1@test.com", accountLink: "all1link", sensorLink: "all1sensor",
            sensorType: SensorType.Level);
        await TestEntityFactory.SeedAccountWithSensor(db.Context,
            email: "all2@test.com", accountLink: "all2link", sensorLink: "all2sensor",
            sensorType: SensorType.Detect);

        var handler = new AddAllDefaultAccountSensorAlarmsCommandHandler(db.Context,
            NullLogger<AddAllDefaultAccountSensorAlarmsCommandHandler>.Instance);

        await handler.Handle(new AddAllDefaultAccountSensorAlarmsCommand(), CancellationToken.None);

        await using var freshCtx = db.CreateFreshContext();
        var alarms = await freshCtx.Set<AccountSensorAlarm>().ToListAsync();

        // Level: Data + PercentageLow = 2, Detect: Data + DetectOn = 2 → total 4
        Assert.Equal(4, alarms.Count);
    }

    [Fact]
    public async Task Handle_NoAccountSensors_Succeeds()
    {
        await using var db = TestDbContext.Create();
        var handler = new AddAllDefaultAccountSensorAlarmsCommandHandler(db.Context,
            NullLogger<AddAllDefaultAccountSensorAlarmsCommandHandler>.Instance);

        // Should not throw
        await handler.Handle(new AddAllDefaultAccountSensorAlarmsCommand(), CancellationToken.None);
    }

    [Fact]
    public async Task Handle_SkipsAccountSensorsWithExistingAlarms()
    {
        await using var db = TestDbContext.Create();
        var (_, _, accountSensor) = await TestEntityFactory.SeedAccountWithSensor(db.Context,
            email: "allskip@test.com", accountLink: "allskiplink", sensorLink: "allskipsensor",
            sensorType: SensorType.Level);

        // Load the Alarms collection so the backing field is populated by EF
        await db.Context.Entry(accountSensor).Collection(a => a.Alarms).LoadAsync();

        // Pre-add an alarm
        accountSensor.AddAlarm(new AccountSensorAlarm
        {
            Uid = Guid.NewGuid(),
            AlarmType = AccountSensorAlarmType.Battery,
            AlarmThreshold = 15.0
        });
        await db.Context.SaveChangesAsync();

        var handler = new AddAllDefaultAccountSensorAlarmsCommandHandler(db.Context,
            NullLogger<AddAllDefaultAccountSensorAlarmsCommandHandler>.Instance);

        await handler.Handle(new AddAllDefaultAccountSensorAlarmsCommand(), CancellationToken.None);

        await using var freshCtx = db.CreateFreshContext();
        var alarms = await freshCtx.Set<AccountSensorAlarm>().ToListAsync();
        // Should still just have original alarm — defaults skipped
        Assert.Single(alarms);
        Assert.Equal(AccountSensorAlarmType.Battery, alarms[0].AlarmType);
    }
}

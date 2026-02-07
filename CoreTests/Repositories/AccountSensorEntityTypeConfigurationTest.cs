using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CoreTests.Repositories;

public class AccountSensorEntityTypeConfigurationTest
{
    [Fact]
    public async Task AccountSensor_TableName_IsAccountSensor()
    {
        await using var db = TestDbContext.Create();
        var entityType = db.Context.Model.FindEntityType(typeof(AccountSensor));
        Assert.NotNull(entityType);
        Assert.Equal("AccountSensor", entityType!.GetTableName());
    }

    [Fact]
    public async Task AccountSensor_HasAlarmsNavigation()
    {
        await using var db = TestDbContext.Create();
        var entityType = db.Context.Model.FindEntityType(typeof(AccountSensor));
        var navigation = entityType!.FindNavigation(nameof(AccountSensor.Alarms));
        Assert.NotNull(navigation);
    }

    [Fact]
    public async Task AccountSensor_Alarms_AreLoadedWithInclude()
    {
        await using var db = TestDbContext.Create();
        var (_, _, accountSensor) = await TestEntityFactory.SeedAccountWithSensor(db.Context,
            email: "aset@test.com", accountLink: "asetlink", sensorLink: "asetsensor");

        var alarm = new AccountSensorAlarm
        {
            Uid = Guid.NewGuid(),
            AlarmType = AccountSensorAlarmType.Battery,
            AccountSensor = accountSensor
        };
        db.Context.Set<AccountSensorAlarm>().Add(alarm);
        await db.Context.SaveChangesAsync();

        await using var freshCtx = db.CreateFreshContext();
        var result = await freshCtx.Set<AccountSensor>()
            .Include(a => a.Alarms)
            .FirstOrDefaultAsync();

        Assert.NotNull(result);
        Assert.Single(result!.Alarms);
    }
}

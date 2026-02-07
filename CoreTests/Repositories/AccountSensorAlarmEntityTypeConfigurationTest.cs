using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CoreTests.Repositories;

public class AccountSensorAlarmEntityTypeConfigurationTest
{
    [Fact]
    public async Task AccountSensorAlarm_TableName_IsAccountSensorAlarm()
    {
        await using var db = TestDbContext.Create();
        var entityType = db.Context.Model.FindEntityType(typeof(AccountSensorAlarm));
        Assert.NotNull(entityType);
        Assert.Equal("AccountSensorAlarm", entityType!.GetTableName());
    }

    [Fact]
    public async Task AccountSensorAlarm_Uid_HasUniqueIndex()
    {
        await using var db = TestDbContext.Create();
        var entityType = db.Context.Model.FindEntityType(typeof(AccountSensorAlarm));
        var uidProperty = entityType!.FindProperty(nameof(AccountSensorAlarm.Uid));
        Assert.NotNull(uidProperty);

        var index = entityType.FindIndex(uidProperty!);
        Assert.NotNull(index);
        Assert.True(index!.IsUnique);
    }

    [Fact]
    public async Task AccountSensorAlarm_HasPrimaryKeyOnId()
    {
        await using var db = TestDbContext.Create();
        var entityType = db.Context.Model.FindEntityType(typeof(AccountSensorAlarm));
        var pk = entityType!.FindPrimaryKey();
        Assert.NotNull(pk);
        Assert.Single(pk!.Properties);
        Assert.Equal(nameof(AccountSensorAlarm.Id), pk.Properties[0].Name);
    }

    [Fact]
    public async Task AccountSensorAlarm_DuplicateUid_ThrowsException()
    {
        await using var db = TestDbContext.Create();
        var (_, _, accountSensor) = await TestEntityFactory.SeedAccountWithSensor(db.Context,
            email: "asadup@test.com", accountLink: "asaduplink", sensorLink: "asadupsensor");

        var uid = Guid.NewGuid();
        db.Context.Set<AccountSensorAlarm>().Add(new AccountSensorAlarm
        {
            Uid = uid,
AlarmType = AccountSensorAlarmType.Data,
            AccountSensor = accountSensor
        });
        await db.Context.SaveChangesAsync();

        db.Context.Set<AccountSensorAlarm>().Add(new AccountSensorAlarm
        {
            Uid = uid,
            AlarmType = AccountSensorAlarmType.Battery,
            AccountSensor = accountSensor
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.Context.SaveChangesAsync());
    }
}

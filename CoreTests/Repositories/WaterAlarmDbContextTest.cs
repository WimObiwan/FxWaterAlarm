using Core.Entities;
using Core.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CoreTests.Repositories;

public class WaterAlarmDbContextTest
{
    [Fact]
    public async Task Constructor_WithSqliteOptions_CreatesInstance()
    {
        await using var db = TestDbContext.Create();
        Assert.NotNull(db.Context);
    }

    [Fact]
    public async Task Accounts_DbSet_IsAvailable()
    {
        await using var db = TestDbContext.Create();
        Assert.NotNull(db.Context.Accounts);
    }

    [Fact]
    public async Task Sensors_DbSet_IsAvailable()
    {
        await using var db = TestDbContext.Create();
        Assert.NotNull(db.Context.Sensors);
    }

    [Fact]
    public async Task Accounts_CanAddAndRetrieve()
    {
        await using var db = TestDbContext.Create();
        var account = TestEntityFactory.CreateAccount("ctx@test.com", "ctxlink");
        db.Context.Accounts.Add(account);
        await db.Context.SaveChangesAsync();

        var retrieved = await db.Context.Accounts.FirstOrDefaultAsync(a => a.Email == "ctx@test.com");
        Assert.NotNull(retrieved);
        Assert.Equal(account.Uid, retrieved!.Uid);
    }

    [Fact]
    public async Task Sensors_CanAddAndRetrieve()
    {
        await using var db = TestDbContext.Create();
        var sensor = TestEntityFactory.CreateSensor(link: "ctxsensor");
        db.Context.Sensors.Add(sensor);
        await db.Context.SaveChangesAsync();

        var retrieved = await db.Context.Sensors.FirstOrDefaultAsync(s => s.Link == "ctxsensor");
        Assert.NotNull(retrieved);
        Assert.Equal(sensor.Uid, retrieved!.Uid);
    }

    [Fact]
    public async Task AccountSensor_JoinEntity_CanBeCreated()
    {
        await using var db = TestDbContext.Create();
        var (account, sensor, accountSensor) = await TestEntityFactory.SeedAccountWithSensor(db.Context,
            email: "join@test.com", accountLink: "joinlink", sensorLink: "joinsensor");

        var result = await db.Context.Set<AccountSensor>()
            .Include(a => a.Account)
            .Include(a => a.Sensor)
            .FirstOrDefaultAsync();

        Assert.NotNull(result);
        Assert.Equal(account.Email, result!.Account.Email);
        Assert.Equal(sensor.Link, result.Sensor.Link);
    }

    [Fact]
    public async Task AccountSensorAlarm_CanBeCreatedAndRetrieved()
    {
        await using var db = TestDbContext.Create();
        var (_, _, accountSensor) = await TestEntityFactory.SeedAccountWithSensor(db.Context,
            email: "alarm@test.com", accountLink: "alarmlink", sensorLink: "alarmsensor");

        var alarm = new AccountSensorAlarm
        {
            Uid = Guid.NewGuid(),
            AlarmType = AccountSensorAlarmType.Data,
            AccountSensor = accountSensor
        };
        db.Context.Set<AccountSensorAlarm>().Add(alarm);
        await db.Context.SaveChangesAsync();

        var result = await db.Context.Set<AccountSensorAlarm>()
            .Include(a => a.AccountSensor)
            .FirstOrDefaultAsync();

        Assert.NotNull(result);
        Assert.Equal(AccountSensorAlarmType.Data, result!.AlarmType);
    }

    [Fact]
    public async Task CreateFreshContext_ReturnsNewContext()
    {
        await using var db = TestDbContext.Create();
        await using var fresh = db.CreateFreshContext();

        Assert.NotNull(fresh);
        Assert.NotSame(db.Context, fresh);
    }
}

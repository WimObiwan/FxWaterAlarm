using Core.Entities;
using Core.Exceptions;
using Core.Queries;
using Xunit;

namespace CoreTests;

public class AccountByEmailQueryHandlerTest
{
    [Fact]
    public async Task Handle_MatchingEmail_ReturnsAccount()
    {
        await using var db = TestDbContext.Create();
        var (account, _, _) = await TestEntityFactory.SeedAccountWithSensor(db.Context, email: "user@test.com");
        var handler = new AccountByEmailQueryHandler(db.Context);

        var result = await handler.Handle(new AccountByEmailQuery { Email = "user@test.com" }, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(account.Uid, result!.Uid);
    }

    [Fact]
    public async Task Handle_NoMatch_ReturnsNull()
    {
        await using var db = TestDbContext.Create();
        var handler = new AccountByEmailQueryHandler(db.Context);

        var result = await handler.Handle(new AccountByEmailQuery { Email = "nonexistent@test.com" }, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_IncludesAccountSensorsAndSensor()
    {
        await using var db = TestDbContext.Create();
        var (account, sensor, _) = await TestEntityFactory.SeedAccountWithSensor(db.Context, email: "inc@test.com");
        var handler = new AccountByEmailQueryHandler(db.Context);

        var result = await handler.Handle(new AccountByEmailQuery { Email = "inc@test.com" }, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result!.AccountSensors);
        Assert.Equal(sensor.Uid, result.AccountSensors.First().Sensor.Uid);
    }
}

public class AccountByLinkQueryHandlerTest
{
    [Fact]
    public async Task Handle_MatchingLink_ReturnsAccount()
    {
        await using var db = TestDbContext.Create();
        var (account, _, _) = await TestEntityFactory.SeedAccountWithSensor(db.Context, accountLink: "mylink");
        var handler = new AccountByLinkQueryHandler(db.Context);

        var result = await handler.Handle(new AccountByLinkQuery { Link = "mylink" }, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(account.Uid, result!.Uid);
    }

    [Fact]
    public async Task Handle_NoMatch_ReturnsNull()
    {
        await using var db = TestDbContext.Create();
        var handler = new AccountByLinkQueryHandler(db.Context);

        var result = await handler.Handle(new AccountByLinkQuery { Link = "nolink" }, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_ExcludesDisabledSensors()
    {
        await using var db = TestDbContext.Create();
        var ctx = db.Context;
        var account = TestEntityFactory.CreateAccount("dis@test.com", "dislink");
        var sensor1 = TestEntityFactory.CreateSensor(link: "s1");
        var sensor2 = TestEntityFactory.CreateSensor(link: "s2");
        ctx.Accounts.Add(account);
        ctx.Sensors.AddRange(sensor1, sensor2);
        await ctx.SaveChangesAsync();

        ctx.Set<AccountSensor>().Add(new AccountSensor
        {
            Account = account, Sensor = sensor1, CreateTimestamp = DateTime.UtcNow, Disabled = false, Order = 0
        });
        ctx.Set<AccountSensor>().Add(new AccountSensor
        {
            Account = account, Sensor = sensor2, CreateTimestamp = DateTime.UtcNow, Disabled = true, Order = 1
        });
        await ctx.SaveChangesAsync();

        // Use fresh context to avoid change tracking interference with filtered includes
        await using var queryCtx = db.CreateFreshContext();
        var handler = new AccountByLinkQueryHandler(queryCtx);
        var result = await handler.Handle(new AccountByLinkQuery { Link = "dislink" }, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result!.AccountSensors);
        Assert.Equal(sensor1.Uid, result.AccountSensors.First().Sensor.Uid);
    }
}

public class AccountQueryHandlerTest
{
    [Fact]
    public async Task Handle_MatchingUid_ReturnsAccount()
    {
        await using var db = TestDbContext.Create();
        var (account, _, _) = await TestEntityFactory.SeedAccountWithSensor(db.Context);
        var handler = new AccountQueryHandler(db.Context);

        var result = await handler.Handle(new AccountQuery { Uid = account.Uid }, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(account.Email, result!.Email);
    }

    [Fact]
    public async Task Handle_NoMatch_ReturnsNull()
    {
        await using var db = TestDbContext.Create();
        var handler = new AccountQueryHandler(db.Context);

        var result = await handler.Handle(new AccountQuery { Uid = Guid.NewGuid() }, CancellationToken.None);

        Assert.Null(result);
    }
}

public class AccountSensorByIdQueryHandlerTest
{
    [Fact]
    public async Task Handle_MatchingIds_ReturnsAccountSensor()
    {
        await using var db = TestDbContext.Create();
        var (account, sensor, _) = await TestEntityFactory.SeedAccountWithSensor(db.Context);
        var handler = new AccountSensorByIdQueryHandler(db.Context);

        var result = await handler.Handle(
            new AccountSensorByIdQuery { AccountUid = account.Uid, SensorUid = sensor.Uid },
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(account.Uid, result!.Account.Uid);
        Assert.Equal(sensor.Uid, result.Sensor.Uid);
    }

    [Fact]
    public async Task Handle_DisabledSensor_ReturnsNull()
    {
        await using var db = TestDbContext.Create();
        var (account, sensor, _) = await TestEntityFactory.SeedAccountWithSensor(db.Context, disabled: true);
        var handler = new AccountSensorByIdQueryHandler(db.Context);

        var result = await handler.Handle(
            new AccountSensorByIdQuery { AccountUid = account.Uid, SensorUid = sensor.Uid },
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_WrongSensorUid_ReturnsNull()
    {
        await using var db = TestDbContext.Create();
        var (account, _, _) = await TestEntityFactory.SeedAccountWithSensor(db.Context);
        var handler = new AccountSensorByIdQueryHandler(db.Context);

        var result = await handler.Handle(
            new AccountSensorByIdQuery { AccountUid = account.Uid, SensorUid = Guid.NewGuid() },
            CancellationToken.None);

        Assert.Null(result);
    }
}

public class AccountSensorByLinkQueryHandlerTest
{
    [Fact]
    public async Task Handle_BySensorLink_ReturnsAccountSensor()
    {
        await using var db = TestDbContext.Create();
        var (_, sensor, _) = await TestEntityFactory.SeedAccountWithSensor(db.Context,
            accountLink: "alink", sensorLink: "slink");
        var handler = new AccountSensorByLinkQueryHandler(db.Context);

        var result = await handler.Handle(
            new AccountSensorByLinkQuery { SensorLink = "slink" },
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(sensor.Uid, result!.Sensor.Uid);
    }

    [Fact]
    public async Task Handle_ByDevEui_ReturnsAccountSensor()
    {
        await using var db = TestDbContext.Create();
        var (_, sensor, _) = await TestEntityFactory.SeedAccountWithSensor(db.Context,
            sensorLink: "slink2", devEui: "mydevice");
        var handler = new AccountSensorByLinkQueryHandler(db.Context);

        var result = await handler.Handle(
            new AccountSensorByLinkQuery { SensorLink = "mydevice" },
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(sensor.Uid, result!.Sensor.Uid);
    }

    [Fact]
    public async Task Handle_WithAccountLink_FiltersCorrectly()
    {
        await using var db = TestDbContext.Create();
        await TestEntityFactory.SeedAccountWithSensor(db.Context,
            email: "a1@test.com", accountLink: "acc1", sensorLink: "shared");
        var handler = new AccountSensorByLinkQueryHandler(db.Context);

        var result = await handler.Handle(
            new AccountSensorByLinkQuery { SensorLink = "shared", AccountLink = "acc1" },
            CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Handle_DisabledSensor_ReturnsNull()
    {
        await using var db = TestDbContext.Create();
        await TestEntityFactory.SeedAccountWithSensor(db.Context, sensorLink: "dlink", disabled: true);
        var handler = new AccountSensorByLinkQueryHandler(db.Context);

        var result = await handler.Handle(
            new AccountSensorByLinkQuery { SensorLink = "dlink" },
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_NoMatch_ReturnsNull()
    {
        await using var db = TestDbContext.Create();
        var handler = new AccountSensorByLinkQueryHandler(db.Context);

        var result = await handler.Handle(
            new AccountSensorByLinkQuery { SensorLink = "nope" },
            CancellationToken.None);

        Assert.Null(result);
    }
}

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

public class AccountSensorsQueryHandlerTest
{
    [Fact]
    public async Task Handle_WithAccountUid_ReturnsAccountSensors()
    {
        await using var db = TestDbContext.Create();
        var (account, _, _) = await TestEntityFactory.SeedAccountWithSensor(db.Context);
        var handler = new AccountSensorsQueryHandler(db.Context);

        var result = await handler.Handle(
            new AccountSensorsQuery { AccountUid = account.Uid },
            CancellationToken.None);

        Assert.Single(result);
    }

    [Fact]
    public async Task Handle_AccountNotFound_ThrowsAccountNotFoundException()
    {
        await using var db = TestDbContext.Create();
        var handler = new AccountSensorsQueryHandler(db.Context);

        await Assert.ThrowsAsync<AccountNotFoundException>(() =>
            handler.Handle(
                new AccountSensorsQuery { AccountUid = Guid.NewGuid() },
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ExcludesDisabledByDefault()
    {
        await using var db = TestDbContext.Create();
        var ctx = db.Context;
        var account = TestEntityFactory.CreateAccount("ex@t.com", "exlink");
        var sensor1 = TestEntityFactory.CreateSensor(link: "es1");
        var sensor2 = TestEntityFactory.CreateSensor(link: "es2");
        ctx.Accounts.Add(account);
        ctx.Sensors.AddRange(sensor1, sensor2);
        await ctx.SaveChangesAsync();

        ctx.Set<AccountSensor>().Add(new AccountSensor
        {
            Account = account, Sensor = sensor1, CreateTimestamp = DateTime.UtcNow, Disabled = false
        });
        ctx.Set<AccountSensor>().Add(new AccountSensor
        {
            Account = account, Sensor = sensor2, CreateTimestamp = DateTime.UtcNow, Disabled = true
        });
        await ctx.SaveChangesAsync();

        // Use fresh context to avoid change tracking interference with filtered includes
        await using var queryCtx = db.CreateFreshContext();
        var handler = new AccountSensorsQueryHandler(queryCtx);
        var result = await handler.Handle(
            new AccountSensorsQuery { AccountUid = account.Uid },
            CancellationToken.None);

        Assert.Single(result);
    }

    [Fact]
    public async Task Handle_IncludeDisabled_ReturnsBoth()
    {
        await using var db = TestDbContext.Create();
        var ctx = db.Context;
        var account = TestEntityFactory.CreateAccount("id@t.com", "idlink");
        var sensor1 = TestEntityFactory.CreateSensor(link: "is1");
        var sensor2 = TestEntityFactory.CreateSensor(link: "is2");
        ctx.Accounts.Add(account);
        ctx.Sensors.AddRange(sensor1, sensor2);
        await ctx.SaveChangesAsync();

        ctx.Set<AccountSensor>().Add(new AccountSensor
        {
            Account = account, Sensor = sensor1, CreateTimestamp = DateTime.UtcNow, Disabled = false
        });
        ctx.Set<AccountSensor>().Add(new AccountSensor
        {
            Account = account, Sensor = sensor2, CreateTimestamp = DateTime.UtcNow, Disabled = true
        });
        await ctx.SaveChangesAsync();

        var handler = new AccountSensorsQueryHandler(ctx);
        var result = await handler.Handle(
            new AccountSensorsQuery { AccountUid = account.Uid, IncludeDisabled = true },
            CancellationToken.None);

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task Handle_NoAccountUid_ReturnsAllAccountSensors()
    {
        await using var db = TestDbContext.Create();
        var ctx = db.Context;
        await TestEntityFactory.SeedAccountWithSensor(ctx, email: "a1@t.com", accountLink: "al1", sensorLink: "sl1");
        await TestEntityFactory.SeedAccountWithSensor(ctx, email: "a2@t.com", accountLink: "al2", sensorLink: "sl2");
        var handler = new AccountSensorsQueryHandler(ctx);

        var result = await handler.Handle(new AccountSensorsQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count());
    }
}

public class AccountsQueryHandlerTest
{
    [Fact]
    public async Task Handle_ReturnsAllAccounts()
    {
        await using var db = TestDbContext.Create();
        var ctx = db.Context;
        ctx.Accounts.Add(TestEntityFactory.CreateAccount("a1@t.com", "l1"));
        ctx.Accounts.Add(TestEntityFactory.CreateAccount("a2@t.com", "l2"));
        await ctx.SaveChangesAsync();
        var handler = new AccountsQueryHandler(ctx);

        var result = await handler.Handle(new AccountsQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task Handle_WithIncludeAccountSensors_IncludesSensors()
    {
        await using var db = TestDbContext.Create();
        await TestEntityFactory.SeedAccountWithSensor(db.Context, email: "ias@t.com", accountLink: "iasl");
        var handler = new AccountsQueryHandler(db.Context);

        var result = await handler.Handle(new AccountsQuery { IncludeAccountSensors = true }, CancellationToken.None);

        Assert.Single(result);
        Assert.Single(result[0].AccountSensors);
    }

    [Fact]
    public async Task Handle_Empty_ReturnsEmptyList()
    {
        await using var db = TestDbContext.Create();
        var handler = new AccountsQueryHandler(db.Context);

        var result = await handler.Handle(new AccountsQuery(), CancellationToken.None);

        Assert.Empty(result);
    }
}

public class SensorByLinkQueryHandlerTest
{
    [Fact]
    public async Task Handle_ByLink_ReturnsSensor()
    {
        await using var db = TestDbContext.Create();
        var sensor = TestEntityFactory.CreateSensor(link: "mysensorlink");
        db.Context.Sensors.Add(sensor);
        await db.Context.SaveChangesAsync();
        var handler = new SensorByLinkQueryHandler(db.Context);

        var result = await handler.Handle(
            new SensorByLinkQuery { SensorLink = "mysensorlink" },
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(sensor.Uid, result!.Uid);
    }

    [Fact]
    public async Task Handle_ByDevEui_ReturnsSensor()
    {
        await using var db = TestDbContext.Create();
        var sensor = TestEntityFactory.CreateSensor(devEui: "devfind");
        db.Context.Sensors.Add(sensor);
        await db.Context.SaveChangesAsync();
        var handler = new SensorByLinkQueryHandler(db.Context);

        var result = await handler.Handle(
            new SensorByLinkQuery { SensorLink = "devfind" },
            CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Handle_NoMatch_ReturnsNull()
    {
        await using var db = TestDbContext.Create();
        var handler = new SensorByLinkQueryHandler(db.Context);

        var result = await handler.Handle(
            new SensorByLinkQuery { SensorLink = "nothing" },
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_WithIncludeAccount_IncludesAccounts()
    {
        await using var db = TestDbContext.Create();
        var (_, sensor, _) = await TestEntityFactory.SeedAccountWithSensor(db.Context, sensorLink: "incalink");
        var handler = new SensorByLinkQueryHandler(db.Context);

        var result = await handler.Handle(
            new SensorByLinkQuery { SensorLink = "incalink", IncludeAccount = true },
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result!.AccountSensors);
    }
}

public class SensorQueryHandlerTest
{
    [Fact]
    public async Task Handle_MatchingUid_ReturnsSensor()
    {
        await using var db = TestDbContext.Create();
        var sensor = TestEntityFactory.CreateSensor();
        db.Context.Sensors.Add(sensor);
        await db.Context.SaveChangesAsync();
        var handler = new SensorQueryHandler(db.Context);

        var result = await handler.Handle(new SensorQuery { Uid = sensor.Uid }, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(sensor.DevEui, result!.DevEui);
    }

    [Fact]
    public async Task Handle_NoMatch_ReturnsNull()
    {
        await using var db = TestDbContext.Create();
        var handler = new SensorQueryHandler(db.Context);

        var result = await handler.Handle(new SensorQuery { Uid = Guid.NewGuid() }, CancellationToken.None);

        Assert.Null(result);
    }
}

public class SensorsQueryHandlerTest
{
    [Fact]
    public async Task Handle_ReturnsAllSensors()
    {
        await using var db = TestDbContext.Create();
        db.Context.Sensors.Add(TestEntityFactory.CreateSensor(link: "sq1"));
        db.Context.Sensors.Add(TestEntityFactory.CreateSensor(link: "sq2"));
        await db.Context.SaveChangesAsync();
        var handler = new SensorsQueryHandler(db.Context);

        var result = await handler.Handle(new SensorsQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task Handle_Empty_ReturnsEmptyList()
    {
        await using var db = TestDbContext.Create();
        var handler = new SensorsQueryHandler(db.Context);

        var result = await handler.Handle(new SensorsQuery(), CancellationToken.None);

        Assert.Empty(result);
    }
}

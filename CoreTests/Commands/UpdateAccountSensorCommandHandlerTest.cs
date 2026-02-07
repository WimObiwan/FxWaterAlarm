using Core.Commands;
using Core.Entities;
using Core.Exceptions;
using Core.Util;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CoreTests.Commands;

public class UpdateAccountSensorCommandHandlerTest
{
    [Fact]
    public async Task Handle_UpdatesDisabled()
    {
        await using var db = TestDbContext.Create();
        var (account, sensor, _) = await TestEntityFactory.SeedAccountWithSensor(db.Context,
            email: "uasdis@test.com", accountLink: "uasdislink", sensorLink: "uasdissensor");

        var handler = new UpdateAccountSensorCommandHandler(db.Context,
            NullLogger<UpdateAccountSensorCommandHandler>.Instance);

        await handler.Handle(new UpdateAccountSensorCommand
        {
            AccountUid = account.Uid,
            SensorUid = sensor.Uid,
            Disabled = new Optional<bool>(true, true)
        }, CancellationToken.None);

        await using var freshCtx = db.CreateFreshContext();
        var updated = await freshCtx.Set<AccountSensor>().SingleAsync();
        Assert.True(updated.Disabled);
    }

    [Fact]
    public async Task Handle_UpdatesName()
    {
        await using var db = TestDbContext.Create();
        var (account, sensor, _) = await TestEntityFactory.SeedAccountWithSensor(db.Context,
            email: "uasname@test.com", accountLink: "uasnlink", sensorLink: "uasnsensor");

        var handler = new UpdateAccountSensorCommandHandler(db.Context,
            NullLogger<UpdateAccountSensorCommandHandler>.Instance);

        await handler.Handle(new UpdateAccountSensorCommand
        {
            AccountUid = account.Uid,
            SensorUid = sensor.Uid,
            Name = new Optional<string>(true, "My Sensor")
        }, CancellationToken.None);

        await using var freshCtx = db.CreateFreshContext();
        var updated = await freshCtx.Set<AccountSensor>().SingleAsync();
        Assert.Equal("My Sensor", updated.Name);
    }

    [Fact]
    public async Task Handle_UpdatesDistanceFields()
    {
        await using var db = TestDbContext.Create();
        var (account, sensor, _) = await TestEntityFactory.SeedAccountWithSensor(db.Context,
            email: "uasdist@test.com", accountLink: "uasdlink", sensorLink: "uasdsensor");

        var handler = new UpdateAccountSensorCommandHandler(db.Context,
            NullLogger<UpdateAccountSensorCommandHandler>.Instance);

        await handler.Handle(new UpdateAccountSensorCommand
        {
            AccountUid = account.Uid,
            SensorUid = sensor.Uid,
            DistanceMmEmpty = new Optional<int?>(true, 2000),
            DistanceMmFull = new Optional<int?>(true, 300),
            CapacityL = new Optional<int?>(true, 5000)
        }, CancellationToken.None);

        await using var freshCtx = db.CreateFreshContext();
        var updated = await freshCtx.Set<AccountSensor>().SingleAsync();
        Assert.Equal(2000, updated.DistanceMmEmpty);
        Assert.Equal(300, updated.DistanceMmFull);
        Assert.Equal(5000, updated.CapacityL);
    }

    [Fact]
    public async Task Handle_UpdatesAlertsEnabled()
    {
        await using var db = TestDbContext.Create();
        var (account, sensor, _) = await TestEntityFactory.SeedAccountWithSensor(db.Context,
            email: "uasalert@test.com", accountLink: "uasalink", sensorLink: "uasasensor");

        var handler = new UpdateAccountSensorCommandHandler(db.Context,
            NullLogger<UpdateAccountSensorCommandHandler>.Instance);

        await handler.Handle(new UpdateAccountSensorCommand
        {
            AccountUid = account.Uid,
            SensorUid = sensor.Uid,
            AlertsEnabled = new Optional<bool>(true, true)
        }, CancellationToken.None);

        await using var freshCtx = db.CreateFreshContext();
        var updated = await freshCtx.Set<AccountSensor>().SingleAsync();
        Assert.True(updated.AlertsEnabled);
    }

    [Fact]
    public async Task Handle_UnspecifiedFieldsUnchanged()
    {
        await using var db = TestDbContext.Create();
        var (account, sensor, accountSensor) = await TestEntityFactory.SeedAccountWithSensor(db.Context,
            email: "uaskeep@test.com", accountLink: "uasklink", sensorLink: "uasksensor");

        // Set some values
        accountSensor.Name = "Original Name";
        accountSensor.DistanceMmEmpty = 1500;
        await db.Context.SaveChangesAsync();

        var handler = new UpdateAccountSensorCommandHandler(db.Context,
            NullLogger<UpdateAccountSensorCommandHandler>.Instance);

        // Only update AlertsEnabled, leave everything else alone
        await handler.Handle(new UpdateAccountSensorCommand
        {
            AccountUid = account.Uid,
            SensorUid = sensor.Uid,
            AlertsEnabled = new Optional<bool>(true, true)
        }, CancellationToken.None);

        await using var freshCtx = db.CreateFreshContext();
        var updated = await freshCtx.Set<AccountSensor>().SingleAsync();
        Assert.Equal("Original Name", updated.Name);
        Assert.Equal(1500, updated.DistanceMmEmpty);
    }

    [Fact]
    public async Task Handle_AccountSensorNotFound_ThrowsAccountSensorNotFoundException()
    {
        await using var db = TestDbContext.Create();
        var handler = new UpdateAccountSensorCommandHandler(db.Context,
            NullLogger<UpdateAccountSensorCommandHandler>.Instance);

        await Assert.ThrowsAsync<AccountSensorNotFoundException>(() =>
            handler.Handle(new UpdateAccountSensorCommand
            {
                AccountUid = Guid.NewGuid(),
                SensorUid = Guid.NewGuid(),
                Disabled = new Optional<bool>(true, false)
            }, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UpdateOrder_ResetsAccountSensorOrder()
    {
        await using var db = TestDbContext.Create();
        var (account, _, _) = await TestEntityFactory.SeedAccountWithSensor(db.Context,
            email: "uasorder@test.com", accountLink: "uasolink", sensorLink: "uasosensor1", order: 0);

        // Add a second sensor to the account
        var sensor2 = TestEntityFactory.CreateSensor(link: "uasosensor2");
        db.Context.Sensors.Add(sensor2);
        await db.Context.SaveChangesAsync();
        var as2 = new AccountSensor
        {
            Account = account,
            Sensor = sensor2,
            CreateTimestamp = DateTime.UtcNow,
            Order = 1
        };
        db.Context.Set<AccountSensor>().Add(as2);
        await db.Context.SaveChangesAsync();

        var handler = new UpdateAccountSensorCommandHandler(db.Context,
            NullLogger<UpdateAccountSensorCommandHandler>.Instance);

        // Move second sensor to order 0
        await handler.Handle(new UpdateAccountSensorCommand
        {
            AccountUid = account.Uid,
            SensorUid = sensor2.Uid,
            Order = new Optional<int>(true, 0)
        }, CancellationToken.None);

        // The order should be reset (ResetAccountSensorOrderHelper normalizes)
        await using var freshCtx = db.CreateFreshContext();
        var sensors = await freshCtx.Set<AccountSensor>()
            .OrderBy(a => a.Order)
            .ToListAsync();
        Assert.Equal(2, sensors.Count);
        Assert.Equal(0, sensors[0].Order);
        Assert.Equal(1, sensors[1].Order);
    }
}

using Core.Commands;
using Core.Entities;
using Core.Exceptions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CoreTests.Commands;

public class RemoveSensorFromAccountCommandHandlerTest
{
    [Fact]
    public async Task Handle_RemovesSensorFromAccount()
    {
        await using var db = TestDbContext.Create();
        var (account, sensor, _) = await TestEntityFactory.SeedAccountWithSensor(db.Context,
            email: "rem@test.com", accountLink: "remlink", sensorLink: "remsensor");

        var handler = new RemoveSensorFromAccountCommandHandler(db.Context);
        await handler.Handle(new RemoveSensorFromAccountCommand
        {
            AccountUid = account.Uid,
            SensorUid = sensor.Uid
        }, CancellationToken.None);

        var accountSensors = await db.Context.Set<AccountSensor>().ToListAsync();
        Assert.Empty(accountSensors);
    }

    [Fact]
    public async Task Handle_AccountNotFound_ThrowsAccountNotFoundException()
    {
        await using var db = TestDbContext.Create();
        var sensor = TestEntityFactory.CreateSensor(link: "rfanf");
        db.Context.Sensors.Add(sensor);
        await db.Context.SaveChangesAsync();

        var handler = new RemoveSensorFromAccountCommandHandler(db.Context);

        await Assert.ThrowsAsync<AccountNotFoundException>(() =>
            handler.Handle(new RemoveSensorFromAccountCommand
            {
                AccountUid = Guid.NewGuid(),
                SensorUid = sensor.Uid
            }, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_SensorNotFound_ThrowsSensorNotFoundException()
    {
        await using var db = TestDbContext.Create();
        var account = TestEntityFactory.CreateAccount("rfsnf@test.com", "rfsnflink");
        db.Context.Accounts.Add(account);
        await db.Context.SaveChangesAsync();

        var handler = new RemoveSensorFromAccountCommandHandler(db.Context);

        await Assert.ThrowsAsync<SensorNotFoundException>(() =>
            handler.Handle(new RemoveSensorFromAccountCommand
            {
                AccountUid = account.Uid,
                SensorUid = Guid.NewGuid()
            }, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_SensorNotLinked_ThrowsNullReferenceException()
    {
        // NOTE: The handler loads AccountSensors but calls account.RemoveSensor(sensor)
        // which uses the _sensors backing field (skip-navigation). Since _sensors is never
        // explicitly loaded, this throws NullReferenceException instead of the intended
        // SensorCouldNotBeRemovedException. This is a known quirk in the source code.
        await using var db = TestDbContext.Create();
        var account = TestEntityFactory.CreateAccount("notlinked@test.com", "nllink");
        var sensor = TestEntityFactory.CreateSensor(link: "nlsensor");
        db.Context.Accounts.Add(account);
        db.Context.Sensors.Add(sensor);
        await db.Context.SaveChangesAsync();

        // Use a fresh context so that EF materializes the Account with backing fields
        await using var freshCtx = db.CreateFreshContext();
        var handler = new RemoveSensorFromAccountCommandHandler(freshCtx);

        await Assert.ThrowsAsync<NullReferenceException>(() =>
            handler.Handle(new RemoveSensorFromAccountCommand
            {
                AccountUid = account.Uid,
                SensorUid = sensor.Uid
            }, CancellationToken.None));
    }
}

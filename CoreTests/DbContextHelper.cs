using Core.Entities;
using Core.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CoreTests;

/// <summary>
/// Wraps a WaterAlarmDbContext with a SQLite in-memory connection.
/// Implements IAsyncDisposable to handle cleanup of both context and connection.
/// </summary>
public sealed class TestDbContext : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    public WaterAlarmDbContext Context { get; }

    private TestDbContext(WaterAlarmDbContext context, SqliteConnection connection)
    {
        Context = context;
        _connection = connection;
    }

    public static TestDbContext Create()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<WaterAlarmDbContext>()
            .UseSqlite(connection)
            .Options;

        var ctx = new WaterAlarmDbContext(options);
        ctx.Database.EnsureCreated();
        return new TestDbContext(ctx, connection);
    }

    /// <summary>
    /// Creates a fresh DbContext on the same database connection.
    /// Useful to avoid change tracking interference with filtered includes.
    /// </summary>
    public WaterAlarmDbContext CreateFreshContext()
    {
        var options = new DbContextOptionsBuilder<WaterAlarmDbContext>()
            .UseSqlite(_connection)
            .Options;

        return new WaterAlarmDbContext(options);
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        await _connection.DisposeAsync();
    }
}

/// <summary>
/// Helper methods for creating test entities.
/// </summary>
public static class TestEntityFactory
{
    public static Account CreateAccount(string email, string? link = null, Guid? uid = null)
    {
        return new Account
        {
            Uid = uid ?? Guid.NewGuid(),
            Email = email,
            CreationTimestamp = DateTime.UtcNow,
            Link = link
        };
    }

    public static Sensor CreateSensor(SensorType type = SensorType.Level, string? link = null, string? devEui = null, Guid? uid = null)
    {
        return new Sensor
        {
            Uid = uid ?? Guid.NewGuid(),
            DevEui = devEui ?? $"dev_{Guid.NewGuid():N}"[..16],
            CreateTimestamp = DateTime.UtcNow,
            Type = type,
            Link = link
        };
    }

    public static async Task<(Account account, Sensor sensor, AccountSensor accountSensor)> SeedAccountWithSensor(
        WaterAlarmDbContext ctx,
        string email = "test@example.com",
        string? accountLink = "acclink",
        string? sensorLink = "sensorlink",
        SensorType sensorType = SensorType.Level,
        bool disabled = false,
        int order = 0,
        string? devEui = null,
        Guid? accountUid = null,
        Guid? sensorUid = null)
    {
        var account = CreateAccount(email, accountLink, accountUid);
        var sensor = CreateSensor(sensorType, sensorLink, devEui, sensorUid);
        ctx.Accounts.Add(account);
        ctx.Sensors.Add(sensor);
        await ctx.SaveChangesAsync();

        var accountSensor = new AccountSensor
        {
            Account = account,
            Sensor = sensor,
            CreateTimestamp = DateTime.UtcNow,
            Order = order,
            Disabled = disabled
        };

        ctx.Set<AccountSensor>().Add(accountSensor);
        await ctx.SaveChangesAsync();

        return (account, sensor, accountSensor);
    }
}

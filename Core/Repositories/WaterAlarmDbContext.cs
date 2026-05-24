using Core.Audit;
using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Design;

namespace Core.Repositories;

public class WaterAlarmDbContext : DbContext
{
    private readonly IAuditService? _auditService;

    public WaterAlarmDbContext(DbContextOptions options)
        : base(options)
    {
    }

    public WaterAlarmDbContext(DbContextOptions options, IAuditService? auditService)
        : base(options)
    {
        _auditService = auditService;
    }

    public DbSet<Account> Accounts { get; init; } = null!;
    public DbSet<Sensor> Sensors { get; init; } = null!;
    public DbSet<AccountUser> AccountUsers { get; init; } = null!;

    protected override void OnModelCreating(ModelBuilder m)
    {
        m.ApplyConfiguration(new AccountEntityTypeConfiguration());
        m.ApplyConfiguration(new SensorEntityTypeConfiguration());
        m.ApplyConfiguration(new AccountSensorEntityTypeConfiguration());
        m.ApplyConfiguration(new AccountSensorAlarmEntityTypeConfiguration());
        m.ApplyConfiguration(new AccountUserEntityTypeConfiguration());

        base.OnModelCreating(m);
    }

    public override int SaveChanges()
    {
        var changes = CaptureAuditChanges();
        var result = base.SaveChanges();
        PersistAuditChangesAsync(changes, CancellationToken.None).GetAwaiter().GetResult();
        return result;
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        var changes = CaptureAuditChanges();
        var result = base.SaveChanges(acceptAllChangesOnSuccess);
        PersistAuditChangesAsync(changes, CancellationToken.None).GetAwaiter().GetResult();
        return result;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var changes = CaptureAuditChanges();
        var result = await base.SaveChangesAsync(cancellationToken);
        await PersistAuditChangesAsync(changes, cancellationToken);
        return result;
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        var changes = CaptureAuditChanges();
        var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        await PersistAuditChangesAsync(changes, cancellationToken);
        return result;
    }

    private List<AuditChange>? CaptureAuditChanges()
    {
        if (_auditService == null)
            return null;

        var entries = ChangeTracker
            .Entries()
            .Where(e =>
                e.Entity != null
                && e.Metadata.ClrType.Namespace != null
                && e.Metadata.ClrType.Namespace.StartsWith("Core.Entities", StringComparison.Ordinal)
                && (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted))
            .ToList();

        if (entries.Count == 0)
            return null;

        var result = new List<AuditChange>();
        foreach (var entry in entries)
        {
            var key = BuildKey(entry);
            var entityName = entry.Metadata.ClrType.Name;

            foreach (var property in entry.Properties)
            {
                if (property.Metadata.IsPrimaryKey())
                    continue;

                if (entry.State == EntityState.Modified)
                {
                    if (!property.IsModified)
                        continue;

                    var oldValue = NormalizeValue(property.OriginalValue);
                    var newValue = NormalizeValue(property.CurrentValue);
                    if (Equals(oldValue, newValue))
                        continue;

                    result.Add(new AuditChange
                    {
                        Entity = entityName,
                        Key = key,
                        Property = property.Metadata.Name,
                        OldValue = oldValue,
                        NewValue = newValue
                    });
                }
                else if (entry.State == EntityState.Added)
                {
                    var newValue = NormalizeValue(property.CurrentValue);
                    if (newValue == null)
                        continue;

                    result.Add(new AuditChange
                    {
                        Entity = entityName,
                        Key = key,
                        Property = property.Metadata.Name,
                        OldValue = null,
                        NewValue = newValue
                    });
                }
                else if (entry.State == EntityState.Deleted)
                {
                    var oldValue = NormalizeValue(property.OriginalValue);
                    if (oldValue == null)
                        continue;

                    result.Add(new AuditChange
                    {
                        Entity = entityName,
                        Key = key,
                        Property = property.Metadata.Name,
                        OldValue = oldValue,
                        NewValue = null
                    });
                }
            }
        }

        return result.Count == 0 ? null : result;
    }

    private async Task PersistAuditChangesAsync(IReadOnlyList<AuditChange>? changes, CancellationToken cancellationToken)
    {
        if (_auditService == null || changes == null || changes.Count == 0)
            return;

        await _auditService.LogAsync(
            AuditOutcome.Succeeded,
            details: new AuditDetails { Message = "Entity changes persisted" },
            changes: changes,
            cancellationToken: cancellationToken);
    }

    private static Dictionary<string, object?> BuildKey(EntityEntry entry)
    {
        var key = new Dictionary<string, object?>();

        var primaryKey = entry.Metadata.FindPrimaryKey();
        if (primaryKey != null)
        {
            foreach (var keyProp in primaryKey.Properties)
            {
                var prop = entry.Property(keyProp.Name);
                key[keyProp.Name] = entry.State == EntityState.Deleted
                    ? NormalizeValue(prop.OriginalValue)
                    : NormalizeValue(prop.CurrentValue);
            }
        }

        switch (entry.Entity)
        {
            case Account account:
                key["accountUid"] = account.Uid;
                break;
            case Sensor sensor:
                key["sensorUid"] = sensor.Uid;
                key["devEui"] = sensor.DevEui;
                break;
            case AccountSensor accountSensor:
                key["accountUid"] = accountSensor.Account?.Uid;
                key["sensorUid"] = accountSensor.Sensor?.Uid;
                break;
            case AccountSensorAlarm accountSensorAlarm:
                key["alarmUid"] = accountSensorAlarm.Uid;
                key["accountUid"] = accountSensorAlarm.AccountSensor?.Account?.Uid;
                key["sensorUid"] = accountSensorAlarm.AccountSensor?.Sensor?.Uid;
                break;
            case AccountUser accountUser:
                key["accountId"] = accountUser.AccountId;
                key["loginType"] = accountUser.LoginType.ToString();
                key["email"] = accountUser.Email;
                break;
        }

        return key;
    }

    private static object? NormalizeValue(object? value)
    {
        if (value is DateTime dt)
            return dt.Kind == DateTimeKind.Utc ? dt : dt.ToUniversalTime();

        return value;
    }
}

public class WaterAlarmDbContextFactory : IDesignTimeDbContextFactory<WaterAlarmDbContext>
{
    public WaterAlarmDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<WaterAlarmDbContext>();
        optionsBuilder.UseSqlite("Data Source=WaterAlarm.db");

        return new WaterAlarmDbContext(optionsBuilder.Options);
    }
}
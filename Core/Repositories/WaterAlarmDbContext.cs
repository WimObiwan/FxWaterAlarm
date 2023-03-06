using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Core.Repositories;

public class WaterAlarmDbContext : DbContext
{
    public WaterAlarmDbContext(DbContextOptions options)
        : base(options)
    {
    }

    public DbSet<Account> Accounts { get; init; } = null!;
    public DbSet<Sensor> Sensors { get; init; } = null!;

    protected override void OnModelCreating(ModelBuilder m)
    {
        m.ApplyConfiguration(new AccountEntityTypeConfiguration());
        m.ApplyConfiguration(new SensorEntityTypeConfiguration());
        m.ApplyConfiguration(new AccountSensorEntityTypeConfiguration());

        base.OnModelCreating(m);
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
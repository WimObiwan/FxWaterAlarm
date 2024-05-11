using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Repositories;

public class AccountSensorEntityTypeConfiguration : IEntityTypeConfiguration<AccountSensor>
{
    public void Configure(EntityTypeBuilder<AccountSensor> builder)
    {
        builder.ToTable("AccountSensor");

        builder
            .HasMany(a => a.Alarms)
            .WithOne(s => s.AccountSensor);
    }
}
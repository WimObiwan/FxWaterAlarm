using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Repositories;

public class AccountEntityTypeConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Account");

        builder.HasKey(a => a.Id);

        builder.HasIndex(e => e.Uid)
            .IsUnique();

        builder
            .HasIndex(e => e.Email)
            .IsUnique();

        builder
            .HasIndex(e => e.Link)
            .IsUnique();

        builder
            .HasMany(a => a.Sensors)
            .WithMany(s => s.Accounts)
            .UsingEntity<AccountSensor>(
                j => j
                    .HasOne(as2 => as2.Sensor)
                    .WithMany(s => s.AccountSensors),
                j => j
                    .HasOne(as2 => as2.Account)
                    .WithMany(a => a.AccountSensors));
    }
}
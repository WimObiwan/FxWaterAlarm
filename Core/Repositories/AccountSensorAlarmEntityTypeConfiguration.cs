using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Repositories;

public class AccountSensorAlarmEntityTypeConfiguration : IEntityTypeConfiguration<AccountSensorAlarm>
{
    public void Configure(EntityTypeBuilder<AccountSensorAlarm> builder)
    {
        builder.ToTable("AccountSensorAlarm");

        builder.HasKey(asa => asa.Id);

        builder.HasIndex(asa => asa.Uid)
            .IsUnique();
    }
}

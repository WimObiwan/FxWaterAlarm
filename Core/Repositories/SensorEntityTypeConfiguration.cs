using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Repositories;

public class SensorEntityTypeConfiguration : IEntityTypeConfiguration<Sensor>
{
    public void Configure(EntityTypeBuilder<Sensor> builder)
    {
        builder.ToTable("Sensor");

        builder.HasKey(a => a.Id);

        builder.HasIndex(e => e.Uid)
            .IsUnique();

        builder
            .HasIndex(e => e.Link)
            .IsUnique();

        builder.Property(e => e.Type)
            .HasDefaultValue(SensorType.Level);

    }
}
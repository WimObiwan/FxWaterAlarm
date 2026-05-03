using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Repositories;

public class AccountUserEntityTypeConfiguration : IEntityTypeConfiguration<AccountUser>
{
    public void Configure(EntityTypeBuilder<AccountUser> builder)
    {
        builder.ToTable("AccountUser");

        builder.HasKey(e => e.Id);

        builder
            .HasOne(e => e.Account)
            .WithMany()
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        // One email per account (mail users); SQLite treats NULLs as distinct so Google rows don't conflict
        builder
            .HasIndex(e => new { e.AccountId, e.LoginType, e.Email })
            .IsUnique();

        // One Google identity globally; SQLite treats NULL pairs as distinct so mail rows don't conflict
        builder
            .HasIndex(e => new { e.Provider, e.ProviderSubjectId })
            .IsUnique();
    }
}

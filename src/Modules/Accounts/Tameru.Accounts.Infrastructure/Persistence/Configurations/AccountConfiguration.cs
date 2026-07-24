using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tameru.Accounts.Domain;

namespace Tameru.Accounts.Infrastructure.Persistence.Configurations;

internal sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(a => a.OpeningBalance).HasColumnType("numeric(19,2)");
        builder.Property(a => a.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(a => a.IsActive).IsRequired();
        builder.Property(a => a.SortOrder).IsRequired();

        builder.HasIndex(a => a.GroupId);
        builder.HasIndex(a => a.IsActive);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tameru.Accounts.Domain;

namespace Tameru.Accounts.Infrastructure.Persistence.Configurations;

internal sealed class AccountGroupConfiguration : IEntityTypeConfiguration<AccountGroup>
{
    public void Configure(EntityTypeBuilder<AccountGroup> builder)
    {
        builder.ToTable("account_groups");
        builder.HasKey(g => g.Id);

        builder.Property(g => g.Name).HasMaxLength(120).IsRequired();
        builder.Property(g => g.SortOrder).IsRequired();
    }
}

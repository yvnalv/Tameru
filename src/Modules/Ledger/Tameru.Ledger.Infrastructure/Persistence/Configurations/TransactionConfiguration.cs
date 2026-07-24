using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tameru.Ledger.Domain;

namespace Tameru.Ledger.Infrastructure.Persistence.Configurations;

internal sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(t => t.Date).IsRequired();
        builder.Property(t => t.Title).HasMaxLength(300).IsRequired();
        builder.Property(t => t.Amount).HasColumnType("numeric(19,2)");
        builder.Property(t => t.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(1000);

        builder.HasIndex(t => new { t.AccountId, t.Date });
        builder.HasIndex(t => new { t.ToAccountId, t.Date });
        builder.HasIndex(t => new { t.CategoryId, t.Date });
        builder.HasIndex(t => t.Date);
    }
}

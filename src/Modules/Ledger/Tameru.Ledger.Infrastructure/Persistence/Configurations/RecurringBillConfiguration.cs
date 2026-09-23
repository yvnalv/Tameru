using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tameru.Ledger.Domain;

namespace Tameru.Ledger.Infrastructure.Persistence.Configurations;

public sealed class RecurringBillConfiguration : IEntityTypeConfiguration<RecurringBill>
{
    public void Configure(EntityTypeBuilder<RecurringBill> builder)
    {
        builder.ToTable("recurring_bills");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Title).HasMaxLength(200).IsRequired();
        builder.Property(b => b.Amount).HasPrecision(19, 2).IsRequired();
        builder.Property(b => b.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(b => b.BillingCycle).HasMaxLength(50).IsRequired();
        builder.Property(b => b.DueDay).IsRequired();
        builder.Property(b => b.AccountId).IsRequired();
        builder.Property(b => b.CategoryId);
        builder.Property(b => b.AutoDebit).IsRequired();
        builder.Property(b => b.IsActive).IsRequired();
        builder.Property(b => b.LastPaidDate);
        builder.Property(b => b.NextDueDate).IsRequired();
        builder.Property(b => b.RemindDaysBefore).IsRequired();
        builder.Property(b => b.Notes).HasMaxLength(1000);

        builder.HasIndex(b => b.IsActive);
        builder.HasIndex(b => b.NextDueDate);
        builder.HasIndex(b => b.AccountId);
    }
}

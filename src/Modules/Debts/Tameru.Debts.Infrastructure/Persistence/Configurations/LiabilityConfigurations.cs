using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tameru.Debts.Domain;

namespace Tameru.Debts.Infrastructure.Persistence.Configurations;

public sealed class LiabilityConfiguration : IEntityTypeConfiguration<Liability>
{
    public void Configure(EntityTypeBuilder<Liability> builder)
    {
        builder.ToTable("liabilities");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Title).HasMaxLength(200).IsRequired();
        builder.Property(l => l.Type).HasMaxLength(50).IsRequired();
        builder.Property(l => l.Creditor).HasMaxLength(200).IsRequired();
        builder.Property(l => l.TotalAmount).HasPrecision(19, 2).IsRequired();
        builder.Property(l => l.PaidAmount).HasPrecision(19, 2).IsRequired();
        builder.Property(l => l.RemainingBalance).HasPrecision(19, 2).IsRequired();
        builder.Property(l => l.MonthlyInstallment).HasPrecision(19, 2).IsRequired();
        builder.Property(l => l.DueDay);
        builder.Property(l => l.InterestRate).HasPrecision(5, 2);
        builder.Property(l => l.StartDate).IsRequired();
        builder.Property(l => l.DueDate);
        builder.Property(l => l.Status).HasMaxLength(50).IsRequired();
        builder.Property(l => l.Notes).HasMaxLength(1000);

        builder.HasMany(l => l.Payments)
            .WithOne()
            .HasForeignKey(p => p.LiabilityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(l => l.Payments)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .AutoInclude();

        builder.HasIndex(l => l.Status);
        builder.HasIndex(l => l.Type);
    }
}

public sealed class LiabilityPaymentConfiguration : IEntityTypeConfiguration<LiabilityPayment>
{
    public void Configure(EntityTypeBuilder<LiabilityPayment> builder)
    {
        builder.ToTable("liability_payments");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.LiabilityId).IsRequired();
        builder.Property(p => p.Date).IsRequired();
        builder.Property(p => p.Amount).HasPrecision(19, 2).IsRequired();
        builder.Property(p => p.PrincipalAmount).HasPrecision(19, 2).IsRequired();
        builder.Property(p => p.InterestAmount).HasPrecision(19, 2).IsRequired();
        builder.Property(p => p.TransactionId);
        builder.Property(p => p.Notes).HasMaxLength(500);

        builder.HasIndex(p => p.LiabilityId);
        builder.HasIndex(p => p.Date);
    }
}

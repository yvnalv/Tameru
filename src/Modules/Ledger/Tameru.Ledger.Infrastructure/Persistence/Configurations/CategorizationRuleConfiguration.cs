using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tameru.Ledger.Domain;

namespace Tameru.Ledger.Infrastructure.Persistence.Configurations;

internal sealed class CategorizationRuleConfiguration : IEntityTypeConfiguration<CategorizationRule>
{
    public void Configure(EntityTypeBuilder<CategorizationRule> builder)
    {
        builder.ToTable("categorization_rules");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name).HasMaxLength(200).IsRequired();
        builder.Property(r => r.Pattern).HasMaxLength(500).IsRequired();
        builder.Property(r => r.MatchField).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(r => r.MatchOperator).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(r => r.TargetStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.Priority).IsRequired();
        builder.Property(r => r.IsActive).IsRequired();
        builder.Property(r => r.MinAmount).HasColumnType("numeric(19,2)");
        builder.Property(r => r.MaxAmount).HasColumnType("numeric(19,2)");
        builder.Property(r => r.AccountId);
        builder.Property(r => r.TransactionType).HasMaxLength(20);
        builder.Property(r => r.ReplaceTitle).HasMaxLength(200);
        builder.Property(r => r.GroupId).HasMaxLength(100);
        builder.Property(r => r.ScheduleExpression).HasMaxLength(200);
        builder.Property(r => r.IsTemplate).IsRequired().HasDefaultValue(false);
        builder.Property(r => r.Tags).HasMaxLength(500);

        builder.HasIndex(r => r.Priority);
        builder.HasIndex(r => r.IsActive);
        builder.HasIndex(r => r.GroupId);
        builder.HasIndex(r => r.IsTemplate);
    }
}

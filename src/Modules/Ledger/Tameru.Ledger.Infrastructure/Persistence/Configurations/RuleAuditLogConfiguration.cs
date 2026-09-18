using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tameru.Ledger.Domain;

namespace Tameru.Ledger.Infrastructure.Persistence.Configurations;

internal sealed class RuleAuditLogConfiguration : IEntityTypeConfiguration<RuleAuditLog>
{
    public void Configure(EntityTypeBuilder<RuleAuditLog> builder)
    {
        builder.ToTable("rule_audit_logs");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.RuleId).IsRequired();
        builder.Property(l => l.RuleName).HasMaxLength(200).IsRequired();
        builder.Property(l => l.TransactionTitle).HasMaxLength(255).IsRequired();
        builder.Property(l => l.Amount).HasColumnType("numeric(19,2)");
        builder.Property(l => l.MatchedField).HasMaxLength(50).IsRequired();
        builder.Property(l => l.MatchedValue).HasMaxLength(500).IsRequired();
        builder.Property(l => l.WasApplied).IsRequired();
        builder.Property(l => l.Details).HasMaxLength(1000);
        builder.Property(l => l.EvaluatedAt).IsRequired();

        builder.HasIndex(l => l.RuleId);
        builder.HasIndex(l => l.TransactionId);
        builder.HasIndex(l => l.EvaluatedAt);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tameru.Budgeting.Domain;

namespace Tameru.Budgeting.Infrastructure.Persistence.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).HasMaxLength(150).IsRequired();
        builder.Property(c => c.Level).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(c => c.Flow).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(c => c.IsSystem).IsRequired();
        builder.Property(c => c.IsActive).IsRequired();
        builder.Property(c => c.SortOrder).IsRequired();

        builder.HasIndex(c => c.Level);
        builder.HasIndex(c => c.ParentId);
    }
}

internal sealed class BudgetConfiguration : IEntityTypeConfiguration<BudgetPeriod>
{
    public void Configure(EntityTypeBuilder<BudgetPeriod> builder)
    {
        builder.ToTable("budget_periods");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Year).IsRequired();
        builder.Property(p => p.Month).IsRequired();
        builder.Property(p => p.Note).HasMaxLength(300);
        builder.HasIndex(p => new { p.Year, p.Month }).IsUnique();
    }
}

internal sealed class BudgetLineConfiguration : IEntityTypeConfiguration<BudgetLine>
{
    public void Configure(EntityTypeBuilder<BudgetLine> builder)
    {
        builder.ToTable("budget_lines");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.PlanAmount).HasColumnType("numeric(19,2)");
        builder.HasIndex(l => new { l.BudgetPeriodId, l.CategoryId }).IsUnique();
    }
}

internal sealed class MasterPlanConfiguration : IEntityTypeConfiguration<MasterPlanSection>
{
    public void Configure(EntityTypeBuilder<MasterPlanSection> builder)
    {
        builder.ToTable("master_plan_sections");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).HasMaxLength(100).IsRequired();
        builder.Property(s => s.TargetPercent).HasColumnType("numeric(5,2)");
    }
}

internal sealed class MasterPlanItemConfiguration : IEntityTypeConfiguration<MasterPlanItem>
{
    public void Configure(EntityTypeBuilder<MasterPlanItem> builder)
    {
        builder.ToTable("master_plan_items");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Name).HasMaxLength(150).IsRequired();
        builder.Property(i => i.Price).HasColumnType("numeric(19,2)");
        builder.Property(i => i.Frequency).IsRequired();
        builder.Ignore(i => i.TotalBudget); // computed = Price × Frequency
        builder.HasIndex(i => i.SectionId);
    }
}

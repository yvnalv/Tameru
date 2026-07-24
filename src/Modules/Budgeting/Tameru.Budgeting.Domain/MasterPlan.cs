using Tameru.SharedKernel.Domain;

namespace Tameru.Budgeting.Domain;

/// <summary>
/// A Master Plan allocation section — Investment / Needs / Wants — each with a target percentage of
/// income (default 40 / 50 / 10 from the workbook, BR-081).
/// </summary>
public sealed class MasterPlanSection : AuditableEntity
{
    private MasterPlanSection()
    {
    }

    private MasterPlanSection(Guid id, string name, decimal targetPercent, int sortOrder) : base(id)
    {
        Name = name;
        TargetPercent = targetPercent;
        SortOrder = sortOrder;
    }

    public string Name { get; private set; } = string.Empty;

    public decimal TargetPercent { get; private set; }

    public int SortOrder { get; private set; }

    public static MasterPlanSection Create(string name, decimal targetPercent, int sortOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainRuleException("section_name_required", "Section name is required.");
        }

        return new MasterPlanSection(Guid.NewGuid(), name.Trim(), Clamp(targetPercent), sortOrder);
    }

    public void SetTarget(decimal targetPercent) => TargetPercent = Clamp(targetPercent);

    private static decimal Clamp(decimal percent) => Math.Clamp(percent, 0m, 100m);
}

/// <summary>
/// A Master Plan item within a section. <c>TotalBudget = Price × Frequency</c> (BR-080, computed).
/// </summary>
public sealed class MasterPlanItem : AuditableEntity
{
    private MasterPlanItem()
    {
    }

    private MasterPlanItem(Guid id, Guid sectionId, string name, decimal price, int frequency, int sortOrder)
        : base(id)
    {
        SectionId = sectionId;
        Name = name;
        Price = price;
        Frequency = frequency;
        SortOrder = sortOrder;
    }

    public Guid SectionId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public decimal Price { get; private set; }

    public int Frequency { get; private set; }

    public int SortOrder { get; private set; }

    public decimal TotalBudget => Price * Frequency;

    public static MasterPlanItem Create(Guid sectionId, string name, decimal price, int frequency, int sortOrder = 0)
    {
        Guard(name, price, frequency);
        return new MasterPlanItem(Guid.NewGuid(), sectionId, name.Trim(), price, frequency, sortOrder);
    }

    public void Update(string name, decimal price, int frequency, int sortOrder)
    {
        Guard(name, price, frequency);
        Name = name.Trim();
        Price = price;
        Frequency = frequency;
        SortOrder = sortOrder;
    }

    private static void Guard(string name, decimal price, int frequency)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainRuleException("item_name_required", "Item name is required.");
        }

        if (price < 0m)
        {
            throw new DomainRuleException("item_price_negative", "Price cannot be negative.");
        }

        if (frequency < 0)
        {
            throw new DomainRuleException("item_frequency_negative", "Frequency cannot be negative.");
        }
    }
}

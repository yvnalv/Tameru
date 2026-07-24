using Tameru.SharedKernel.Domain;

namespace Tameru.Accounts.Domain;

/// <summary>
/// A label that groups accounts for roll-ups (Saving, Investment, Family, …), from the workbook's
/// account-group tags. Master data: edit + deactivate, never physical delete.
/// </summary>
public sealed class AccountGroup : AuditableEntity
{
    private AccountGroup()
    {
    }

    private AccountGroup(Guid id, string name, int sortOrder) : base(id)
    {
        Name = name;
        SortOrder = sortOrder;
    }

    public string Name { get; private set; } = string.Empty;

    public int SortOrder { get; private set; }

    public static AccountGroup Create(string name, int sortOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainRuleException("group_name_required", "Account group name is required.");
        }

        return new AccountGroup(Guid.NewGuid(), name.Trim(), sortOrder);
    }

    public void Update(string name, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainRuleException("group_name_required", "Account group name is required.");
        }

        Name = name.Trim();
        SortOrder = sortOrder;
    }
}

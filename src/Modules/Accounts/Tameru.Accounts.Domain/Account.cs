using Tameru.SharedKernel.Domain;
using Tameru.SharedKernel.ValueObjects;

namespace Tameru.Accounts.Domain;

/// <summary>
/// A money container (cash, bank, e-wallet, investment, blocked). Its live balance is never stored;
/// it is derived from the ledger as <c>OpeningBalance + net movement</c> (ADR-0006, BR-022).
/// </summary>
public sealed class Account : AuditableEntity
{
    private Account()
    {
    }

    private Account(
        Guid id, string name, Guid? groupId, AccountType type,
        decimal openingBalance, string currencyCode, int sortOrder)
        : base(id)
    {
        Name = name;
        GroupId = groupId;
        Type = type;
        OpeningBalance = openingBalance;
        CurrencyCode = currencyCode;
        SortOrder = sortOrder;
        IsActive = true;
    }

    public string Name { get; private set; } = string.Empty;

    public Guid? GroupId { get; private set; }

    public AccountType Type { get; private set; }

    public decimal OpeningBalance { get; private set; }

    public string CurrencyCode { get; private set; } = Money.FunctionalCurrency;

    public bool IsActive { get; private set; }

    public int SortOrder { get; private set; }

    public static Account Create(
        string name,
        AccountType type,
        decimal openingBalance = 0m,
        Guid? groupId = null,
        string? currencyCode = null,
        int sortOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainRuleException("account_name_required", "Account name is required.");
        }

        var currency = Money.Create(openingBalance, currencyCode ?? Money.FunctionalCurrency).Currency;
        return new Account(Guid.NewGuid(), name.Trim(), groupId, type, openingBalance, currency, sortOrder);
    }

    public void Update(
        string name, AccountType type, decimal openingBalance,
        Guid? groupId, string? currencyCode, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainRuleException("account_name_required", "Account name is required.");
        }

        Name = name.Trim();
        Type = type;
        OpeningBalance = openingBalance;
        GroupId = groupId;
        CurrencyCode = Money.Create(openingBalance, currencyCode ?? CurrencyCode).Currency;
        SortOrder = sortOrder;
    }

    /// <summary>Deactivates the account (soft state). Callers must first ensure it is not in use (BR-021).</summary>
    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    /// <summary>Current balance given the net ledger movement for this account (ADR-0006).</summary>
    public decimal BalanceWith(decimal netMovement) => OpeningBalance + netMovement;
}

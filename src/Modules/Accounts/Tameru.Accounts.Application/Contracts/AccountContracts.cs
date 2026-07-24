namespace Tameru.Accounts.Application.Contracts;

/// <summary>An account with its derived current balance (docs/API_SPEC.md → Accounts).</summary>
public sealed record AccountDto(
    Guid Id,
    string Name,
    Guid? GroupId,
    string? GroupName,
    string Type,
    decimal OpeningBalance,
    decimal Balance,
    string CurrencyCode,
    bool IsActive,
    int SortOrder);

/// <summary>An account group with roll-up totals across its accounts.</summary>
public sealed record AccountGroupDto(
    Guid Id,
    string Name,
    int SortOrder,
    int AccountCount,
    decimal TotalBalance);

public sealed record CreateAccountRequest(
    string Name,
    string Type,
    decimal OpeningBalance,
    Guid? GroupId,
    string? CurrencyCode,
    int SortOrder);

public sealed record UpdateAccountRequest(
    string Name,
    string Type,
    decimal OpeningBalance,
    Guid? GroupId,
    string? CurrencyCode,
    int SortOrder);

public sealed record CreateAccountGroupRequest(string Name, int SortOrder);

public sealed record UpdateAccountGroupRequest(string Name, int SortOrder);

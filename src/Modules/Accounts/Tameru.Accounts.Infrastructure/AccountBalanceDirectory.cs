using Tameru.Accounts.Application;
using Tameru.Modules.Contracts.Accounts;

namespace Tameru.Accounts.Infrastructure;

/// <summary>
/// Implements the cross-module <see cref="IAccountBalanceDirectory"/> contract by delegating to
/// <see cref="AccountService"/>, so the derived-balance formula (opening + net movement, ADR-0006)
/// lives in exactly one place. Consumed by Reporting for net worth (BR-023).
/// </summary>
internal sealed class AccountBalanceDirectory : IAccountBalanceDirectory
{
    private readonly AccountService _accounts;

    public AccountBalanceDirectory(AccountService accounts) => _accounts = accounts;

    public async Task<IReadOnlyList<AccountBalance>> GetBalancesAsync(
        bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        var accounts = await _accounts.ListAsync(includeInactive: !activeOnly, cancellationToken);
        return accounts
            .Select(a => new AccountBalance(
                a.Id, a.Name, a.GroupName, a.Type, a.CurrencyCode, a.Balance, a.IsActive))
            .ToList();
    }
}

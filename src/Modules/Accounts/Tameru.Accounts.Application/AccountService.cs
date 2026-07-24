using Tameru.Accounts.Application.Abstractions;
using Tameru.Accounts.Application.Contracts;
using Tameru.Accounts.Domain;
using Tameru.Modules.Contracts.Ledger;
using Tameru.SharedKernel.Results;

namespace Tameru.Accounts.Application;

/// <summary>
/// Use cases for accounts and account groups. Balances are derived from the ledger
/// (<see cref="ILedgerAccountQuery"/>) as <c>opening + net movement</c> (ADR-0006); they are never
/// stored. Deactivation is guarded against accounts still referenced by transactions (BR-021).
/// </summary>
public sealed class AccountService
{
    private readonly IAccountRepository _accounts;
    private readonly IAccountGroupRepository _groups;
    private readonly ILedgerAccountQuery _ledger;
    private readonly IAccountsUnitOfWork _unitOfWork;

    public AccountService(
        IAccountRepository accounts,
        IAccountGroupRepository groups,
        ILedgerAccountQuery ledger,
        IAccountsUnitOfWork unitOfWork)
    {
        _accounts = accounts;
        _groups = groups;
        _ledger = ledger;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<AccountDto>> ListAsync(bool includeInactive, CancellationToken ct = default)
    {
        var accounts = await _accounts.ListAsync(includeInactive, ct);
        var groups = await _groups.ListAsync(ct);
        var movements = await _ledger.GetNetMovementByAccountAsync(cancellationToken: ct);
        var groupNames = groups.ToDictionary(g => g.Id, g => g.Name);

        return accounts
            .Select(a => Map(a, groupNames, movements))
            .ToList();
    }

    public async Task<Result<AccountDto>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var account = await _accounts.GetByIdAsync(id, ct);
        if (account is null)
        {
            return AccountErrors.AccountNotFound;
        }

        var groups = await _groups.ListAsync(ct);
        var movement = await _ledger.GetNetMovementAsync(id, cancellationToken: ct);
        return Map(account, groups.ToDictionary(g => g.Id, g => g.Name),
            new Dictionary<Guid, decimal> { [id] = movement });
    }

    public async Task<Result<AccountDto>> CreateAsync(CreateAccountRequest request, CancellationToken ct = default)
    {
        if (!TryParseType(request.Type, out var type))
        {
            return AccountErrors.InvalidType(request.Type);
        }

        if (request.GroupId is { } groupId && await _groups.GetByIdAsync(groupId, ct) is null)
        {
            return AccountErrors.GroupNotFound;
        }

        var account = Account.Create(
            request.Name, type, request.OpeningBalance, request.GroupId, request.CurrencyCode, request.SortOrder);

        await _accounts.AddAsync(account, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return await GetAsync(account.Id, ct);
    }

    public async Task<Result<AccountDto>> UpdateAsync(
        Guid id, UpdateAccountRequest request, CancellationToken ct = default)
    {
        var account = await _accounts.GetByIdAsync(id, ct);
        if (account is null)
        {
            return AccountErrors.AccountNotFound;
        }

        if (!TryParseType(request.Type, out var type))
        {
            return AccountErrors.InvalidType(request.Type);
        }

        if (request.GroupId is { } groupId && await _groups.GetByIdAsync(groupId, ct) is null)
        {
            return AccountErrors.GroupNotFound;
        }

        account.Update(
            request.Name, type, request.OpeningBalance, request.GroupId, request.CurrencyCode, request.SortOrder);
        await _unitOfWork.SaveChangesAsync(ct);
        return await GetAsync(account.Id, ct);
    }

    public async Task<Result> DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var account = await _accounts.GetByIdAsync(id, ct);
        if (account is null)
        {
            return AccountErrors.AccountNotFound;
        }

        if (await _ledger.HasTransactionsAsync(id, ct))
        {
            return AccountErrors.AccountInUse;
        }

        account.Deactivate();
        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<IReadOnlyList<AccountGroupDto>> ListGroupsAsync(CancellationToken ct = default)
    {
        var groups = await _groups.ListAsync(ct);
        var accounts = await _accounts.ListAsync(includeInactive: true, ct);
        var movements = await _ledger.GetNetMovementByAccountAsync(cancellationToken: ct);

        return groups
            .Select(g =>
            {
                var members = accounts.Where(a => a.GroupId == g.Id).ToList();
                var total = members.Sum(a => a.BalanceWith(movements.GetValueOrDefault(a.Id)));
                return new AccountGroupDto(g.Id, g.Name, g.SortOrder, members.Count, total);
            })
            .ToList();
    }

    public async Task<Result<AccountGroupDto>> CreateGroupAsync(
        CreateAccountGroupRequest request, CancellationToken ct = default)
    {
        var group = AccountGroup.Create(request.Name, request.SortOrder);
        await _groups.AddAsync(group, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return new AccountGroupDto(group.Id, group.Name, group.SortOrder, 0, 0m);
    }

    public async Task<Result<AccountGroupDto>> UpdateGroupAsync(
        Guid id, UpdateAccountGroupRequest request, CancellationToken ct = default)
    {
        var group = await _groups.GetByIdAsync(id, ct);
        if (group is null)
        {
            return AccountErrors.GroupNotFound;
        }

        group.Update(request.Name, request.SortOrder);
        await _unitOfWork.SaveChangesAsync(ct);
        return new AccountGroupDto(group.Id, group.Name, group.SortOrder, 0, 0m);
    }

    private static AccountDto Map(
        Account a, IReadOnlyDictionary<Guid, string> groupNames, IReadOnlyDictionary<Guid, decimal> movements)
    {
        var groupName = a.GroupId is { } gid && groupNames.TryGetValue(gid, out var name) ? name : null;
        var balance = a.BalanceWith(movements.GetValueOrDefault(a.Id));
        return new AccountDto(
            a.Id, a.Name, a.GroupId, groupName, a.Type.ToString(),
            a.OpeningBalance, balance, a.CurrencyCode, a.IsActive, a.SortOrder);
    }

    private static bool TryParseType(string value, out AccountType type) =>
        Enum.TryParse(value, ignoreCase: true, out type) && Enum.IsDefined(type);
}

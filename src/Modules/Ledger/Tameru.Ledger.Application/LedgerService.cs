using Tameru.Ledger.Application.Abstractions;
using Tameru.Ledger.Application.Contracts;
using Tameru.Ledger.Domain;
using Tameru.Modules.Contracts.Accounts;
using Tameru.SharedKernel.Results;

namespace Tameru.Ledger.Application;

/// <summary>
/// Use cases for the cashflow ledger: create/update Income, Expense, Transfer; clear/unclear; void
/// (soft-delete); and list. Referenced accounts are validated through the Accounts module's
/// <see cref="IAccountDirectory"/> contract (docs/ARCHITECTURE.md). Money rules live in the domain
/// (BR-001..003) and surface as <c>DomainRuleException</c>s mapped to 422.
/// </summary>
public sealed class LedgerService
{
    private readonly ITransactionRepository _transactions;
    private readonly IAccountDirectory _accounts;
    private readonly ILedgerUnitOfWork _unitOfWork;

    public LedgerService(
        ITransactionRepository transactions, IAccountDirectory accounts, ILedgerUnitOfWork unitOfWork)
    {
        _transactions = transactions;
        _accounts = accounts;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PagedResult<TransactionDto>>> ListAsync(
        TransactionFilter filter, CancellationToken ct = default)
    {
        var page = await _transactions.ListAsync(filter, ct);
        var items = page.Items.Select(Map).ToList();
        return new PagedResult<TransactionDto>(items, page.Page, page.PageSize, page.Total);
    }

    public async Task<Result<TransactionDto>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var transaction = await _transactions.GetByIdAsync(id, ct);
        return transaction is null ? LedgerErrors.TransactionNotFound : Map(transaction);
    }

    public async Task<Result<TransactionDto>> CreateAsync(CreateTransactionRequest request, CancellationToken ct = default)
    {
        if (!TryParseType(request.Type, out var type))
        {
            return LedgerErrors.InvalidType(request.Type);
        }

        if (!TryParseStatus(request.Status, out var status))
        {
            return LedgerErrors.InvalidStatus(request.Status!);
        }

        var accountsOk = await ValidateAccountsAsync(type, request.AccountId, request.ToAccountId, ct);
        if (accountsOk.IsFailure)
        {
            return accountsOk.Error;
        }

        var transaction = type switch
        {
            TransactionType.Income => Transaction.Income(
                request.Date, request.Title, request.Amount, request.AccountId, status,
                request.BudgetCategoryId, request.CategoryId, request.SubCategoryId,
                request.CurrencyCode, request.Description),
            TransactionType.Expense => Transaction.Expense(
                request.Date, request.Title, request.Amount, request.AccountId, status,
                request.BudgetCategoryId, request.CategoryId, request.SubCategoryId,
                request.CurrencyCode, request.Description),
            _ => Transaction.Transfer(
                request.Date, request.Title, request.Amount, request.AccountId,
                request.ToAccountId ?? Guid.Empty, status, request.CurrencyCode, request.Description),
        };

        await _transactions.AddAsync(transaction, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Map(transaction);
    }

    public async Task<Result<TransactionDto>> UpdateAsync(
        Guid id, UpdateTransactionRequest request, CancellationToken ct = default)
    {
        var transaction = await _transactions.GetByIdAsync(id, ct);
        if (transaction is null)
        {
            return LedgerErrors.TransactionNotFound;
        }

        if (!TryParseStatus(request.Status, out var status))
        {
            return LedgerErrors.InvalidStatus(request.Status!);
        }

        var accountsOk = await ValidateAccountsAsync(transaction.Type, request.AccountId, request.ToAccountId, ct);
        if (accountsOk.IsFailure)
        {
            return accountsOk.Error;
        }

        transaction.UpdateCommon(request.Date, request.Title, request.Amount, status, request.Description);

        if (transaction.Type == TransactionType.Transfer)
        {
            transaction.ReassignTransfer(request.AccountId, request.ToAccountId ?? Guid.Empty);
        }
        else
        {
            transaction.ReassignAccount(request.AccountId);
            transaction.ReassignCategories(request.BudgetCategoryId, request.CategoryId, request.SubCategoryId);
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return Map(transaction);
    }

    public Task<Result<TransactionDto>> ClearAsync(Guid id, CancellationToken ct = default) =>
        SetStatusAsync(id, clear: true, ct);

    public Task<Result<TransactionDto>> UnclearAsync(Guid id, CancellationToken ct = default) =>
        SetStatusAsync(id, clear: false, ct);

    public async Task<Result> VoidAsync(Guid id, CancellationToken ct = default)
    {
        var transaction = await _transactions.GetByIdAsync(id, ct);
        if (transaction is null)
        {
            return LedgerErrors.TransactionNotFound;
        }

        _transactions.Remove(transaction); // soft-delete (void, BR-007)
        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    private async Task<Result<TransactionDto>> SetStatusAsync(Guid id, bool clear, CancellationToken ct)
    {
        var transaction = await _transactions.GetByIdAsync(id, ct);
        if (transaction is null)
        {
            return LedgerErrors.TransactionNotFound;
        }

        if (clear)
        {
            transaction.Clear();
        }
        else
        {
            transaction.Unclear();
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return Map(transaction);
    }

    private async Task<Result> ValidateAccountsAsync(
        TransactionType type, Guid accountId, Guid? toAccountId, CancellationToken ct)
    {
        if (!await _accounts.ExistsAndActiveAsync(accountId, ct))
        {
            return LedgerErrors.AccountNotFound;
        }

        if (type == TransactionType.Transfer)
        {
            if (toAccountId is not { } target || !await _accounts.ExistsAndActiveAsync(target, ct))
            {
                return LedgerErrors.AccountNotFound;
            }
        }

        return Result.Success();
    }

    private static TransactionDto Map(Transaction t) => new(
        t.Id, t.Type.ToString(), t.Date, t.Title, t.Amount, t.CurrencyCode,
        t.AccountId, t.ToAccountId, t.BudgetCategoryId, t.CategoryId, t.SubCategoryId,
        t.Status.ToString(), t.Description);

    private static bool TryParseType(string value, out TransactionType type) =>
        Enum.TryParse(value, ignoreCase: true, out type) && Enum.IsDefined(type);

    private static bool TryParseStatus(string? value, out TransactionStatus status)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            status = TransactionStatus.Uncleared;
            return true;
        }

        return Enum.TryParse(value, ignoreCase: true, out status) && Enum.IsDefined(status);
    }
}

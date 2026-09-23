using Tameru.Ledger.Application.Abstractions;
using Tameru.Ledger.Application.Contracts;
using Tameru.Ledger.Domain;
using Tameru.Modules.Contracts.Accounts;
using Tameru.Modules.Contracts.Budgeting;
using Tameru.SharedKernel.Results;
using Tameru.SharedKernel.Time;

namespace Tameru.Ledger.Application;

public sealed class RecurringBillService
{
    private readonly IRecurringBillRepository _bills;
    private readonly LedgerService _ledgerService;
    private readonly IAccountDirectory _accounts;
    private readonly ICategoryDirectory? _categories;
    private readonly ILedgerUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RecurringBillService(
        IRecurringBillRepository bills,
        LedgerService ledgerService,
        IAccountDirectory accounts,
        ILedgerUnitOfWork unitOfWork,
        IClock clock,
        ICategoryDirectory? categories = null)
    {
        _bills = bills;
        _ledgerService = ledgerService;
        _accounts = accounts;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _categories = categories;
    }

    public async Task<Result<IReadOnlyList<RecurringBillDto>>> ListAsync(
        string? filter = null,
        CancellationToken ct = default)
    {
        var bills = await _bills.ListAsync(null, ct);
        var today = DateOnly.FromDateTime(_clock.UtcNow.DateTime);

        var accounts = await _accounts.ListActiveAccountsAsync(ct);
        var accountMap = accounts.ToDictionary(a => a.Id, a => a.Name);

        var dtos = bills.Select(b => Map(b, today, accountMap)).ToList();

        if (string.Equals(filter, "overdue", StringComparison.OrdinalIgnoreCase))
        {
            dtos = dtos.Where(d => d.IsOverdue).ToList();
        }
        else if (string.Equals(filter, "due_soon", StringComparison.OrdinalIgnoreCase))
        {
            dtos = dtos.Where(d => d.IsDueSoon || d.IsOverdue).ToList();
        }
        else if (string.Equals(filter, "paid", StringComparison.OrdinalIgnoreCase))
        {
            dtos = dtos.Where(d => d.IsPaidThisCycle).ToList();
        }
        else if (string.Equals(filter, "active", StringComparison.OrdinalIgnoreCase))
        {
            dtos = dtos.Where(d => d.IsActive).ToList();
        }

        return Result<IReadOnlyList<RecurringBillDto>>.Of(
            dtos.OrderBy(d => d.IsPaidThisCycle ? 1 : 0)
                .ThenBy(d => d.NextDueDate)
                .ThenBy(d => d.Title)
                .ToList());
    }

    public async Task<Result<RecurringBillDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var bill = await _bills.GetByIdAsync(id, ct);
        if (bill is null)
            return Error.NotFound("Recurring bill not found.");

        var today = DateOnly.FromDateTime(_clock.UtcNow.DateTime);
        var accounts = await _accounts.ListActiveAccountsAsync(ct);
        var accountMap = accounts.ToDictionary(a => a.Id, a => a.Name);

        return Map(bill, today, accountMap);
    }

    public async Task<Result<RecurringBillDto>> CreateAsync(CreateRecurringBillRequest request, CancellationToken ct = default)
    {
        var bill = RecurringBill.Create(
            title: request.Title,
            amount: request.Amount,
            billingCycle: request.BillingCycle,
            dueDay: request.DueDay,
            accountId: request.AccountId,
            categoryId: request.CategoryId,
            autoDebit: request.AutoDebit,
            remindDaysBefore: request.RemindDaysBefore,
            startDate: request.StartDate,
            currencyCode: request.CurrencyCode,
            notes: request.Notes);

        await _bills.AddAsync(bill, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var today = DateOnly.FromDateTime(_clock.UtcNow.DateTime);
        var accounts = await _accounts.ListActiveAccountsAsync(ct);
        var accountMap = accounts.ToDictionary(a => a.Id, a => a.Name);

        return Map(bill, today, accountMap);
    }

    public async Task<Result<RecurringBillDto>> UpdateAsync(Guid id, UpdateRecurringBillRequest request, CancellationToken ct = default)
    {
        var bill = await _bills.GetByIdAsync(id, ct);
        if (bill is null)
            return Error.NotFound("Recurring bill not found.");

        bill.Update(
            title: request.Title,
            amount: request.Amount,
            billingCycle: request.BillingCycle,
            dueDay: request.DueDay,
            accountId: request.AccountId,
            categoryId: request.CategoryId,
            autoDebit: request.AutoDebit,
            isActive: request.IsActive,
            remindDaysBefore: request.RemindDaysBefore,
            notes: request.Notes);

        await _unitOfWork.SaveChangesAsync(ct);

        var today = DateOnly.FromDateTime(_clock.UtcNow.DateTime);
        var accounts = await _accounts.ListActiveAccountsAsync(ct);
        var accountMap = accounts.ToDictionary(a => a.Id, a => a.Name);

        return Map(bill, today, accountMap);
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var bill = await _bills.GetByIdAsync(id, ct);
        if (bill is null)
            return Error.NotFound("Recurring bill not found.");

        _bills.Remove(bill);
        await _unitOfWork.SaveChangesAsync(ct);
        return true;
    }

    public async Task<Result<PayRecurringBillResult>> PayAsync(
        Guid id,
        PayRecurringBillRequest request,
        CancellationToken ct = default)
    {
        var bill = await _bills.GetByIdAsync(id, ct);
        if (bill is null)
            return Error.NotFound("Recurring bill not found.");

        var amount = request.Amount ?? bill.Amount;
        var accountId = request.AccountId ?? bill.AccountId;
        var categoryId = request.CategoryId ?? bill.CategoryId;

        var txRequest = new CreateTransactionRequest(
            Type: "Expense",
            Date: request.Date,
            Title: bill.Title,
            Amount: amount,
            AccountId: accountId,
            ToAccountId: null,
            BudgetCategoryId: null,
            CategoryId: categoryId,
            SubCategoryId: null,
            Status: "Cleared",
            CurrencyCode: bill.CurrencyCode,
            Description: request.Notes ?? $"Recurring payment: {bill.Title}");

        var txResult = await _ledgerService.CreateAsync(txRequest, ct);
        if (txResult.IsFailure)
        {
            return txResult.Error;
        }

        bill.RecordPayment(request.Date);
        await _unitOfWork.SaveChangesAsync(ct);

        var today = DateOnly.FromDateTime(_clock.UtcNow.DateTime);
        var accounts = await _accounts.ListActiveAccountsAsync(ct);
        var accountMap = accounts.ToDictionary(a => a.Id, a => a.Name);

        return new PayRecurringBillResult(txResult.Value, Map(bill, today, accountMap));
    }

    public async Task<Result<RecurringBillsSummaryDto>> GetSummaryAsync(CancellationToken ct = default)
    {
        var bills = await _bills.ListAsync(activeOnly: true, ct);
        var today = DateOnly.FromDateTime(_clock.UtcNow.DateTime);

        decimal monthlyCommitment = 0m;
        int overdueCount = 0;
        decimal overdueAmount = 0m;
        int dueSoonCount = 0;
        decimal dueSoonAmount = 0m;

        foreach (var b in bills)
        {
            monthlyCommitment += NormalizeToMonthly(b.Amount, b.BillingCycle);

            bool isPaidThisCycle = IsPaidCurrentCycle(b, today);
            if (!isPaidThisCycle)
            {
                if (today > b.NextDueDate)
                {
                    overdueCount++;
                    overdueAmount += b.Amount;
                }
                else if (b.NextDueDate <= today.AddDays(7))
                {
                    dueSoonCount++;
                    dueSoonAmount += b.Amount;
                }
            }
        }

        return new RecurringBillsSummaryDto(
            TotalActiveBills: bills.Count,
            TotalMonthlyCommitment: Math.Round(monthlyCommitment, 2),
            OverdueCount: overdueCount,
            OverdueAmount: overdueAmount,
            DueSoonCount: dueSoonCount,
            DueSoonAmount: dueSoonAmount);
    }

    private static decimal NormalizeToMonthly(decimal amount, string cycle)
    {
        return cycle.ToLowerInvariant() switch
        {
            "weekly" => amount * 4.33m,
            "quarterly" => amount / 3m,
            "yearly" => amount / 12m,
            _ => amount
        };
    }

    private static bool IsPaidCurrentCycle(RecurringBill b, DateOnly today)
    {
        if (!b.LastPaidDate.HasValue) return false;

        var last = b.LastPaidDate.Value;
        return b.BillingCycle.ToLowerInvariant() switch
        {
            "weekly" => (today.DayNumber - last.DayNumber) < 7,
            "quarterly" => (today.Year == last.Year && (today.Month - 1) / 3 == (last.Month - 1) / 3) ||
                           (today.DayNumber - last.DayNumber) < 90,
            "yearly" => today.Year == last.Year,
            _ => today.Year == last.Year && today.Month == last.Month
        };
    }

    private static RecurringBillDto Map(
        RecurringBill b,
        DateOnly today,
        IReadOnlyDictionary<Guid, string> accountMap)
    {
        bool isPaidThisCycle = IsPaidCurrentCycle(b, today);
        bool isOverdue = !isPaidThisCycle && today > b.NextDueDate;
        bool isDueSoon = !isPaidThisCycle && !isOverdue && b.NextDueDate <= today.AddDays(b.RemindDaysBefore > 0 ? b.RemindDaysBefore : 7);
        int daysUntilDue = b.NextDueDate.DayNumber - today.DayNumber;

        accountMap.TryGetValue(b.AccountId, out var accountName);

        return new RecurringBillDto(
            Id: b.Id,
            Title: b.Title,
            Amount: b.Amount,
            CurrencyCode: b.CurrencyCode,
            BillingCycle: b.BillingCycle,
            DueDay: b.DueDay,
            AccountId: b.AccountId,
            AccountName: accountName,
            CategoryId: b.CategoryId,
            CategoryName: null,
            AutoDebit: b.AutoDebit,
            IsActive: b.IsActive,
            LastPaidDate: b.LastPaidDate,
            NextDueDate: b.NextDueDate,
            RemindDaysBefore: b.RemindDaysBefore,
            Notes: b.Notes,
            IsOverdue: isOverdue,
            IsDueSoon: isDueSoon,
            IsPaidThisCycle: isPaidThisCycle,
            DaysUntilDue: daysUntilDue);
    }
}

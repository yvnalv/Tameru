using Tameru.Ledger.Application.Contracts;

namespace Tameru.Ledger.Application.Contracts;

public sealed record RecurringBillDto(
    Guid Id,
    string Title,
    decimal Amount,
    string CurrencyCode,
    string BillingCycle,
    int DueDay,
    Guid AccountId,
    string? AccountName,
    Guid? CategoryId,
    string? CategoryName,
    bool AutoDebit,
    bool IsActive,
    DateOnly? LastPaidDate,
    DateOnly NextDueDate,
    int RemindDaysBefore,
    string? Notes,
    bool IsOverdue,
    bool IsDueSoon,
    bool IsPaidThisCycle,
    int DaysUntilDue);

public sealed record RecurringBillsSummaryDto(
    int TotalActiveBills,
    decimal TotalMonthlyCommitment,
    int OverdueCount,
    decimal OverdueAmount,
    int DueSoonCount,
    decimal DueSoonAmount);

public sealed record CreateRecurringBillRequest(
    string Title,
    decimal Amount,
    string BillingCycle,
    int DueDay,
    Guid AccountId,
    Guid? CategoryId = null,
    bool AutoDebit = false,
    int RemindDaysBefore = 3,
    DateOnly? StartDate = null,
    string? CurrencyCode = "IDR",
    string? Notes = null);

public sealed record UpdateRecurringBillRequest(
    string Title,
    decimal Amount,
    string BillingCycle,
    int DueDay,
    Guid AccountId,
    Guid? CategoryId = null,
    bool AutoDebit = false,
    bool IsActive = true,
    int RemindDaysBefore = 3,
    string? Notes = null);

public sealed record PayRecurringBillRequest(
    DateOnly Date,
    decimal? Amount = null,
    Guid? AccountId = null,
    Guid? CategoryId = null,
    string? Notes = null);

public sealed record PayRecurringBillResult(
    TransactionDto Transaction,
    RecurringBillDto RecurringBill);

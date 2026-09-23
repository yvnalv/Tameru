using Tameru.SharedKernel.Domain;

namespace Tameru.Ledger.Domain;

public static class BillingCycle
{
    public const string Monthly = "Monthly";
    public const string Quarterly = "Quarterly";
    public const string Yearly = "Yearly";
    public const string Weekly = "Weekly";

    public static readonly string[] All = [Monthly, Quarterly, Yearly, Weekly];

    public static bool IsValid(string? cycle) =>
        cycle is not null && All.Contains(cycle, StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// A recurring cashflow commitment (subscription, utility bill, insurance, membership).
/// Automatically schedules due dates and allows 1-click execution to log directly into ledger transactions.
/// </summary>
public sealed class RecurringBill : AuditableEntity
{
    private RecurringBill()
    {
    }

    private RecurringBill(
        Guid id,
        string title,
        decimal amount,
        string currencyCode,
        string billingCycle,
        int dueDay,
        Guid accountId,
        Guid? categoryId,
        bool autoDebit,
        bool isActive,
        DateOnly? lastPaidDate,
        DateOnly nextDueDate,
        int remindDaysBefore,
        string? notes)
        : base(id)
    {
        Title = title;
        Amount = amount;
        CurrencyCode = currencyCode;
        BillingCycle = billingCycle;
        DueDay = dueDay;
        AccountId = accountId;
        CategoryId = categoryId;
        AutoDebit = autoDebit;
        IsActive = isActive;
        LastPaidDate = lastPaidDate;
        NextDueDate = nextDueDate;
        RemindDaysBefore = remindDaysBefore;
        Notes = notes;
    }

    public string Title { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string CurrencyCode { get; private set; } = "IDR";
    public string BillingCycle { get; private set; } = Domain.BillingCycle.Monthly;
    public int DueDay { get; private set; } = 1;
    public Guid AccountId { get; private set; }
    public Guid? CategoryId { get; private set; }
    public bool AutoDebit { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateOnly? LastPaidDate { get; private set; }
    public DateOnly NextDueDate { get; private set; }
    public int RemindDaysBefore { get; private set; } = 3;
    public string? Notes { get; private set; }

    public static RecurringBill Create(
        string title,
        decimal amount,
        string billingCycle,
        int dueDay,
        Guid accountId,
        Guid? categoryId = null,
        bool autoDebit = false,
        int remindDaysBefore = 3,
        DateOnly? startDate = null,
        string? currencyCode = "IDR",
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainRuleException("recurring_bill_title_required", "Recurring bill title is required.");

        if (amount <= 0)
            throw new DomainRuleException("recurring_bill_amount_must_be_positive", "Amount must be greater than zero.");

        if (!Domain.BillingCycle.IsValid(billingCycle))
            throw new DomainRuleException("invalid_billing_cycle", $"Invalid billing cycle '{billingCycle}'.");

        if (dueDay < 1 || dueDay > 31)
            throw new DomainRuleException("invalid_due_day", "Due day must be between 1 and 31.");

        if (accountId == Guid.Empty)
            throw new DomainRuleException("recurring_bill_account_required", "A valid paying account is required.");

        var referenceDate = startDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var nextDue = CalculateInitialDueDate(referenceDate, billingCycle, dueDay);

        return new RecurringBill(
            Guid.NewGuid(),
            title.Trim(),
            amount,
            currencyCode ?? "IDR",
            billingCycle,
            dueDay,
            accountId,
            categoryId,
            autoDebit,
            isActive: true,
            lastPaidDate: null,
            nextDueDate: nextDue,
            remindDaysBefore: remindDaysBefore,
            notes: notes?.Trim());
    }

    public void Update(
        string title,
        decimal amount,
        string billingCycle,
        int dueDay,
        Guid accountId,
        Guid? categoryId,
        bool autoDebit,
        bool isActive,
        int remindDaysBefore,
        string? notes)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainRuleException("recurring_bill_title_required", "Recurring bill title is required.");

        if (amount <= 0)
            throw new DomainRuleException("recurring_bill_amount_must_be_positive", "Amount must be greater than zero.");

        if (!Domain.BillingCycle.IsValid(billingCycle))
            throw new DomainRuleException("invalid_billing_cycle", $"Invalid billing cycle '{billingCycle}'.");

        if (dueDay < 1 || dueDay > 31)
            throw new DomainRuleException("invalid_due_day", "Due day must be between 1 and 31.");

        if (accountId == Guid.Empty)
            throw new DomainRuleException("recurring_bill_account_required", "A valid paying account is required.");

        Title = title.Trim();
        Amount = amount;
        BillingCycle = billingCycle;
        DueDay = dueDay;
        AccountId = accountId;
        CategoryId = categoryId;
        AutoDebit = autoDebit;
        IsActive = isActive;
        RemindDaysBefore = remindDaysBefore;
        Notes = notes?.Trim();

        // Recalculate next due date if needed
        if (LastPaidDate.HasValue)
        {
            NextDueDate = CalculateNextDueDate(LastPaidDate.Value, BillingCycle, DueDay);
        }
    }

    public void RecordPayment(DateOnly paymentDate)
    {
        LastPaidDate = paymentDate;
        NextDueDate = CalculateNextDueDate(paymentDate, BillingCycle, DueDay);
    }

    public static DateOnly CalculateInitialDueDate(DateOnly fromDate, string cycle, int dueDay)
    {
        int daysInMonth = DateTime.DaysInMonth(fromDate.Year, fromDate.Month);
        int targetDay = Math.Min(dueDay, daysInMonth);
        var targetThisMonth = new DateOnly(fromDate.Year, fromDate.Month, targetDay);

        if (targetThisMonth >= fromDate)
            return targetThisMonth;

        return CalculateNextDueDate(fromDate, cycle, dueDay);
    }

    public static DateOnly CalculateNextDueDate(DateOnly baseDate, string cycle, int dueDay)
    {
        return cycle.ToLowerInvariant() switch
        {
            "weekly" => baseDate.AddDays(7),
            "quarterly" => AddMonthsWithDay(baseDate, 3, dueDay),
            "yearly" => AddMonthsWithDay(baseDate, 12, dueDay),
            _ => AddMonthsWithDay(baseDate, 1, dueDay)
        };
    }

    private static DateOnly AddMonthsWithDay(DateOnly date, int monthsToAdd, int dueDay)
    {
        var targetMonthDate = date.AddMonths(monthsToAdd);
        int maxDay = DateTime.DaysInMonth(targetMonthDate.Year, targetMonthDate.Month);
        int day = Math.Min(dueDay, maxDay);
        return new DateOnly(targetMonthDate.Year, targetMonthDate.Month, day);
    }
}

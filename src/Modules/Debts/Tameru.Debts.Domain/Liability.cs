using Tameru.SharedKernel.Domain;

namespace Tameru.Debts.Domain;

/// <summary>
/// A personal liability, debt, installment, or receivable (Financial Projection Sheet 27 'Liabilities').
/// Tracks total principal, payments made, remaining balance, and repayment pace.
/// </summary>
public sealed class Liability : AuditableEntity
{
    private readonly List<LiabilityPayment> _payments = [];

    private Liability()
    {
    }

    private Liability(
        Guid id,
        string title,
        string type,
        string creditor,
        decimal totalAmount,
        decimal paidAmount,
        decimal monthlyInstallment,
        int? dueDay,
        decimal? interestRate,
        DateOnly startDate,
        DateOnly? dueDate,
        string status,
        string? notes)
        : base(id)
    {
        Title = title;
        Type = type;
        Creditor = creditor;
        TotalAmount = totalAmount;
        PaidAmount = paidAmount;
        RemainingBalance = Math.Max(0m, totalAmount - paidAmount);
        MonthlyInstallment = monthlyInstallment;
        DueDay = dueDay;
        InterestRate = interestRate;
        StartDate = startDate;
        DueDate = dueDate;
        Status = status;
        Notes = notes;
    }

    public string Title { get; private set; } = string.Empty;
    public string Type { get; private set; } = LiabilityType.Debt;
    public string Creditor { get; private set; } = string.Empty;
    public decimal TotalAmount { get; private set; }
    public decimal PaidAmount { get; private set; }
    public decimal RemainingBalance { get; private set; }
    public decimal MonthlyInstallment { get; private set; }
    public int? DueDay { get; private set; }
    public decimal? InterestRate { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public string Status { get; private set; } = LiabilityStatus.Active;
    public string? Notes { get; private set; }

    public IReadOnlyCollection<LiabilityPayment> Payments => _payments.AsReadOnly();

    public static Liability Create(
        string title,
        string type,
        string creditor,
        decimal totalAmount,
        decimal initialPaidAmount = 0m,
        decimal monthlyInstallment = 0m,
        int? dueDay = null,
        decimal? interestRate = null,
        DateOnly? startDate = null,
        DateOnly? dueDate = null,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainRuleException("liability_title_required", "Liability title is required.");

        if (!LiabilityType.IsValid(type))
            throw new DomainRuleException("invalid_liability_type", $"Invalid liability type '{type}'.");

        if (totalAmount <= 0)
            throw new DomainRuleException("liability_amount_must_be_positive", "Total liability amount must be greater than zero.");

        if (initialPaidAmount < 0)
            throw new DomainRuleException("liability_paid_cannot_be_negative", "Initial paid amount cannot be negative.");

        if (dueDay.HasValue && (dueDay.Value < 1 || dueDay.Value > 31))
            throw new DomainRuleException("invalid_due_day", "Due day must be between 1 and 31.");

        var remaining = Math.Max(0m, totalAmount - initialPaidAmount);
        var status = remaining == 0 ? LiabilityStatus.PaidOff : LiabilityStatus.Active;

        return new Liability(
            Guid.NewGuid(),
            title.Trim(),
            type,
            creditor.Trim(),
            totalAmount,
            initialPaidAmount,
            monthlyInstallment,
            dueDay,
            interestRate,
            startDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            dueDate,
            status,
            notes?.Trim());
    }

    public void Update(
        string title,
        string type,
        string creditor,
        decimal totalAmount,
        decimal monthlyInstallment,
        int? dueDay,
        decimal? interestRate,
        DateOnly startDate,
        DateOnly? dueDate,
        string? notes)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainRuleException("liability_title_required", "Liability title is required.");

        if (!LiabilityType.IsValid(type))
            throw new DomainRuleException("invalid_liability_type", $"Invalid liability type '{type}'.");

        if (totalAmount <= 0)
            throw new DomainRuleException("liability_amount_must_be_positive", "Total liability amount must be greater than zero.");

        if (dueDay.HasValue && (dueDay.Value < 1 || dueDay.Value > 31))
            throw new DomainRuleException("invalid_due_day", "Due day must be between 1 and 31.");

        Title = title.Trim();
        Type = type;
        Creditor = creditor.Trim();
        TotalAmount = totalAmount;
        MonthlyInstallment = monthlyInstallment;
        DueDay = dueDay;
        InterestRate = interestRate;
        StartDate = startDate;
        DueDate = dueDate;
        Notes = notes?.Trim();

        RecalculateBalance();
    }

    public LiabilityPayment RecordPayment(
        decimal amount,
        DateOnly date,
        decimal? principal = null,
        decimal? interest = null,
        Guid? transactionId = null,
        string? notes = null)
    {
        if (amount <= 0)
            throw new DomainRuleException("payment_amount_must_be_positive", "Payment amount must be greater than zero.");

        var principalAmount = principal ?? amount;
        var interestAmount = interest ?? 0m;

        var payment = LiabilityPayment.Create(
            Id,
            date,
            amount,
            principalAmount,
            interestAmount,
            transactionId,
            notes);

        _payments.Add(payment);
        RecalculateBalance();

        return payment;
    }

    public void RemovePayment(Guid paymentId)
    {
        var payment = _payments.FirstOrDefault(p => p.Id == paymentId);
        if (payment is not null)
        {
            _payments.Remove(payment);
            RecalculateBalance();
        }
    }

    public void RecalculateBalance()
    {
        var paymentPrincipalSum = _payments.Sum(p => p.PrincipalAmount);
        PaidAmount = paymentPrincipalSum;
        RemainingBalance = Math.Max(0m, TotalAmount - PaidAmount);

        if (RemainingBalance == 0)
        {
            Status = LiabilityStatus.PaidOff;
        }
        else if (Status == LiabilityStatus.PaidOff)
        {
            Status = LiabilityStatus.Active;
        }
    }
}

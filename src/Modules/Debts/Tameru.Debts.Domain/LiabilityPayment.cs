using Tameru.SharedKernel.Domain;

namespace Tameru.Debts.Domain;

/// <summary>
/// An individual payment installment applied toward a liability.
/// </summary>
public sealed class LiabilityPayment : AuditableEntity
{
    private LiabilityPayment()
    {
    }

    private LiabilityPayment(
        Guid id,
        Guid liabilityId,
        DateOnly date,
        decimal amount,
        decimal principalAmount,
        decimal interestAmount,
        Guid? transactionId,
        string? notes)
        : base(id)
    {
        LiabilityId = liabilityId;
        Date = date;
        Amount = amount;
        PrincipalAmount = principalAmount;
        InterestAmount = interestAmount;
        TransactionId = transactionId;
        Notes = notes;
    }

    public Guid LiabilityId { get; private set; }
    public DateOnly Date { get; private set; }
    public decimal Amount { get; private set; }
    public decimal PrincipalAmount { get; private set; }
    public decimal InterestAmount { get; private set; }
    public Guid? TransactionId { get; private set; }
    public string? Notes { get; private set; }

    public static LiabilityPayment Create(
        Guid liabilityId,
        DateOnly date,
        decimal amount,
        decimal principalAmount,
        decimal interestAmount = 0m,
        Guid? transactionId = null,
        string? notes = null)
    {
        if (amount <= 0)
            throw new DomainRuleException("payment_amount_must_be_positive", "Payment amount must be greater than zero.");

        return new LiabilityPayment(
            Guid.NewGuid(),
            liabilityId,
            date,
            amount,
            principalAmount,
            interestAmount,
            transactionId,
            notes?.Trim());
    }
}

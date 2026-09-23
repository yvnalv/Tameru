using Tameru.Debts.Application.Abstractions;
using Tameru.Debts.Application.Contracts;
using Tameru.Debts.Domain;
using Tameru.Modules.Contracts.Ledger;
using Tameru.SharedKernel.Results;

namespace Tameru.Debts.Application;

public sealed class DebtsService
{
    private readonly ILiabilityRepository _liabilities;
    private readonly IDebtsUnitOfWork _unitOfWork;
    private readonly ITransactionIngestor? _transactionIngestor;

    public DebtsService(
        ILiabilityRepository liabilities,
        IDebtsUnitOfWork unitOfWork,
        ITransactionIngestor? transactionIngestor = null)
    {
        _liabilities = liabilities;
        _unitOfWork = unitOfWork;
        _transactionIngestor = transactionIngestor;
    }

    public async Task<Result<IReadOnlyList<LiabilityDto>>> ListAsync(
        string? status = null,
        string? type = null,
        CancellationToken ct = default)
    {
        var items = await _liabilities.ListAsync(status, type, ct);
        return Result<IReadOnlyList<LiabilityDto>>.Of(items.Select(Map).ToList());
    }

    public async Task<Result<LiabilityDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _liabilities.GetByIdAsync(id, ct);
        return item is null ? DebtsErrors.LiabilityNotFound : Map(item);
    }

    public async Task<Result<IReadOnlyList<LiabilityPaymentDto>>> GetPaymentsAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _liabilities.GetByIdAsync(id, ct);
        if (item is null)
            return DebtsErrors.LiabilityNotFound;

        return Result<IReadOnlyList<LiabilityPaymentDto>>.Of(item.Payments
            .OrderByDescending(p => p.Date)
            .ThenByDescending(p => p.CreatedAt)
            .Select(MapPayment)
            .ToList());
    }

    public async Task<Result<LiabilityDto>> CreateAsync(CreateLiabilityRequest request, CancellationToken ct = default)
    {
        var liability = Liability.Create(
            title: request.Title,
            type: request.Type,
            creditor: request.Creditor,
            totalAmount: request.TotalAmount,
            initialPaidAmount: request.InitialPaidAmount,
            monthlyInstallment: request.MonthlyInstallment,
            dueDay: request.DueDay,
            interestRate: request.InterestRate,
            startDate: request.StartDate,
            dueDate: request.DueDate,
            notes: request.Notes);

        if (request.InitialPaidAmount > 0)
        {
            liability.RecordPayment(
                amount: request.InitialPaidAmount,
                date: request.StartDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
                principal: request.InitialPaidAmount,
                notes: "Initial balance payment");
        }

        await _liabilities.AddAsync(liability, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Map(liability);
    }

    public async Task<Result<LiabilityDto>> UpdateAsync(Guid id, UpdateLiabilityRequest request, CancellationToken ct = default)
    {
        var liability = await _liabilities.GetByIdAsync(id, ct);
        if (liability is null)
            return DebtsErrors.LiabilityNotFound;

        liability.Update(
            title: request.Title,
            type: request.Type,
            creditor: request.Creditor,
            totalAmount: request.TotalAmount,
            monthlyInstallment: request.MonthlyInstallment,
            dueDay: request.DueDay,
            interestRate: request.InterestRate,
            startDate: request.StartDate,
            dueDate: request.DueDate,
            notes: request.Notes);

        await _unitOfWork.SaveChangesAsync(ct);
        return Map(liability);
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var liability = await _liabilities.GetByIdAsync(id, ct);
        if (liability is null)
            return DebtsErrors.LiabilityNotFound;

        _liabilities.Remove(liability);
        await _unitOfWork.SaveChangesAsync(ct);
        return true;
    }

    public async Task<Result<LiabilityPaymentDto>> RecordPaymentAsync(
        Guid id,
        RecordLiabilityPaymentRequest request,
        CancellationToken ct = default)
    {
        var liability = await _liabilities.GetByIdAsync(id, ct);
        if (liability is null)
            return DebtsErrors.LiabilityNotFound;

        Guid? transactionId = null;

        if (request.PostToLedger && request.AccountId.HasValue && _transactionIngestor is not null)
        {
            var outcome = await _transactionIngestor.IngestAsync(new IngestCommand(
                Text: null,
                Amount: request.Amount,
                Title: $"{liability.Title} ({liability.Creditor})",
                Type: "Expense",
                AccountId: request.AccountId.Value,
                CategoryId: request.CategoryId,
                Date: request.Date,
                Description: request.Notes ?? $"Repayment installment for {liability.Title}"), ct);

            if (outcome is not null)
            {
                transactionId = outcome.TransactionId;
            }
        }

        var payment = liability.RecordPayment(
            amount: request.Amount,
            date: request.Date,
            principal: request.PrincipalAmount,
            interest: request.InterestAmount,
            transactionId: transactionId,
            notes: request.Notes);

        await _liabilities.AddPaymentAsync(payment, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return MapPayment(payment);
    }

    public async Task<Result<bool>> DeletePaymentAsync(Guid liabilityId, Guid paymentId, CancellationToken ct = default)
    {
        var liability = await _liabilities.GetByIdAsync(liabilityId, ct);
        if (liability is null)
            return DebtsErrors.LiabilityNotFound;

        var payment = liability.Payments.FirstOrDefault(p => p.Id == paymentId);
        if (payment is null)
            return DebtsErrors.PaymentNotFound;

        liability.RemovePayment(paymentId);
        _liabilities.RemovePayment(payment);
        await _unitOfWork.SaveChangesAsync(ct);
        return true;
    }

    public async Task<Result<DebtsSummaryDto>> GetSummaryAsync(CancellationToken ct = default)
    {
        var all = await _liabilities.ListAsync(null, null, ct);

        var debts = all.Where(l => l.Type != LiabilityType.Receivable).ToList();
        var receivables = all.Where(l => l.Type == LiabilityType.Receivable).ToList();

        var totalDebts = debts.Sum(l => l.TotalAmount);
        var totalPaid = debts.Sum(l => l.PaidAmount);
        var totalRemaining = debts.Where(l => l.Status == LiabilityStatus.Active).Sum(l => l.RemainingBalance);
        var monthlyCommitment = debts.Where(l => l.Status == LiabilityStatus.Active).Sum(l => l.MonthlyInstallment);
        var totalReceivables = receivables.Where(l => l.Status == LiabilityStatus.Active).Sum(l => l.RemainingBalance);

        var activeCount = all.Count(l => l.Status == LiabilityStatus.Active);
        var paidOffCount = all.Count(l => l.Status == LiabilityStatus.PaidOff);
        var progress = totalDebts > 0 ? Math.Round((totalPaid / totalDebts) * 100m, 1) : 100m;

        return new DebtsSummaryDto(
            TotalDebts: totalDebts,
            TotalPaid: totalPaid,
            TotalRemaining: totalRemaining,
            MonthlyCommitment: monthlyCommitment,
            TotalReceivables: totalReceivables,
            ActiveCount: activeCount,
            PaidOffCount: paidOffCount,
            OverallProgressPercentage: progress);
    }

    private static LiabilityDto Map(Liability l)
    {
        var progress = l.TotalAmount > 0
            ? Math.Min(100m, Math.Round((l.PaidAmount / l.TotalAmount) * 100m, 1))
            : 100m;

        return new LiabilityDto(
            Id: l.Id,
            Title: l.Title,
            Type: l.Type,
            Creditor: l.Creditor,
            TotalAmount: l.TotalAmount,
            PaidAmount: l.PaidAmount,
            RemainingBalance: l.RemainingBalance,
            MonthlyInstallment: l.MonthlyInstallment,
            DueDay: l.DueDay,
            InterestRate: l.InterestRate,
            StartDate: l.StartDate,
            DueDate: l.DueDate,
            Status: l.Status,
            Notes: l.Notes,
            ProgressPercentage: progress,
            PaymentsCount: l.Payments.Count);
    }

    private static LiabilityPaymentDto MapPayment(LiabilityPayment p) =>
        new(
            Id: p.Id,
            LiabilityId: p.LiabilityId,
            Date: p.Date,
            Amount: p.Amount,
            PrincipalAmount: p.PrincipalAmount,
            InterestAmount: p.InterestAmount,
            TransactionId: p.TransactionId,
            Notes: p.Notes);
}

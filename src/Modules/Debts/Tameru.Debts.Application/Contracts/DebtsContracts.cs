namespace Tameru.Debts.Application.Contracts;

public sealed record LiabilityDto(
    Guid Id,
    string Title,
    string Type,
    string Creditor,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal RemainingBalance,
    decimal MonthlyInstallment,
    int? DueDay,
    decimal? InterestRate,
    DateOnly StartDate,
    DateOnly? DueDate,
    string Status,
    string? Notes,
    decimal ProgressPercentage,
    int PaymentsCount);

public sealed record LiabilityPaymentDto(
    Guid Id,
    Guid LiabilityId,
    DateOnly Date,
    decimal Amount,
    decimal PrincipalAmount,
    decimal InterestAmount,
    Guid? TransactionId,
    string? Notes);

public sealed record DebtsSummaryDto(
    decimal TotalDebts,
    decimal TotalPaid,
    decimal TotalRemaining,
    decimal MonthlyCommitment,
    decimal TotalReceivables,
    int ActiveCount,
    int PaidOffCount,
    decimal OverallProgressPercentage);

public sealed record CreateLiabilityRequest(
    string Title,
    string Type,
    string Creditor,
    decimal TotalAmount,
    decimal InitialPaidAmount = 0m,
    decimal MonthlyInstallment = 0m,
    int? DueDay = null,
    decimal? InterestRate = null,
    DateOnly? StartDate = null,
    DateOnly? DueDate = null,
    string? Notes = null);

public sealed record UpdateLiabilityRequest(
    string Title,
    string Type,
    string Creditor,
    decimal TotalAmount,
    decimal MonthlyInstallment = 0m,
    int? DueDay = null,
    decimal? InterestRate = null,
    DateOnly StartDate = default,
    DateOnly? DueDate = null,
    string? Notes = null);

public sealed record RecordLiabilityPaymentRequest(
    decimal Amount,
    DateOnly Date,
    decimal? PrincipalAmount = null,
    decimal? InterestAmount = null,
    string? Notes = null,
    bool PostToLedger = false,
    Guid? AccountId = null,
    Guid? CategoryId = null);

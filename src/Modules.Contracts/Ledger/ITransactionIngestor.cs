namespace Tameru.Modules.Contracts.Ledger;

public sealed record IngestCommand(
    string? Text,
    decimal? Amount = null,
    string? Title = null,
    string? Type = null,
    Guid? AccountId = null,
    Guid? CategoryId = null,
    DateOnly? Date = null,
    string? Description = null);

public sealed record IngestOutcome(
    Guid TransactionId,
    string Title,
    decimal Amount,
    string Type,
    string ConfirmationMessage,
    string? AppliedRuleName,
    Guid? AccountId = null,
    string? AccountName = null,
    Guid? CategoryId = null,
    string? CategoryName = null,
    Guid? BudgetCategoryId = null,
    string? BudgetName = null,
    DateOnly? Date = null,
    string? Status = null,
    string? Description = null);

public sealed record UpdateIngestCommand(
    Guid Id,
    DateOnly? Date = null,
    string? Title = null,
    decimal? Amount = null,
    Guid? AccountId = null,
    Guid? BudgetCategoryId = null,
    Guid? CategoryId = null,
    Guid? SubCategoryId = null,
    string? Status = null,
    string? Description = null);

public interface ITransactionIngestor
{
    Task<IngestOutcome?> IngestAsync(IngestCommand command, CancellationToken ct = default);
    Task<IngestOutcome?> UpdateAsync(UpdateIngestCommand command, CancellationToken ct = default);
}

using Tameru.Ledger.Application;
using Tameru.Ledger.Application.Contracts;
using Tameru.Modules.Contracts.Ledger;

namespace Tameru.Ledger.Infrastructure;

internal sealed class TransactionIngestor : ITransactionIngestor
{
    private readonly IngestionService _ingestion;

    public TransactionIngestor(IngestionService ingestion)
    {
        _ingestion = ingestion;
    }

    public async Task<IngestOutcome?> IngestAsync(IngestCommand command, CancellationToken ct = default)
    {
        var req = new IngestTransactionRequest(
            Text: command.Text,
            Amount: command.Amount,
            Title: command.Title,
            Type: command.Type,
            AccountId: command.AccountId,
            CategoryId: command.CategoryId,
            Date: command.Date,
            Description: command.Description);

        var result = await _ingestion.IngestAsync(req, ct);
        if (result.IsFailure)
        {
            return null;
        }

        var val = result.Value;
        return new IngestOutcome(
            val.Transaction.Id,
            val.Transaction.Title,
            val.Transaction.Amount,
            val.Transaction.Type,
            val.FormattedConfirmation,
            val.AppliedRuleName,
            AccountId: val.Transaction.AccountId,
            AccountName: val.AccountName,
            CategoryId: val.Transaction.CategoryId,
            CategoryName: val.CategoryName,
            BudgetCategoryId: val.Transaction.BudgetCategoryId,
            BudgetName: val.BudgetName,
            Date: val.Transaction.Date,
            Status: val.Transaction.Status,
            Description: val.Transaction.Description);
    }

    public async Task<IngestOutcome?> UpdateAsync(UpdateIngestCommand command, CancellationToken ct = default)
    {
        var result = await _ingestion.UpdateAsync(command, ct);
        if (result.IsFailure)
        {
            return null;
        }

        var val = result.Value;
        return new IngestOutcome(
            val.Transaction.Id,
            val.Transaction.Title,
            val.Transaction.Amount,
            val.Transaction.Type,
            val.FormattedConfirmation,
            val.AppliedRuleName,
            AccountId: val.Transaction.AccountId,
            AccountName: val.AccountName,
            CategoryId: val.Transaction.CategoryId,
            CategoryName: val.CategoryName,
            BudgetCategoryId: val.Transaction.BudgetCategoryId,
            BudgetName: val.BudgetName,
            Date: val.Transaction.Date,
            Status: val.Transaction.Status,
            Description: val.Transaction.Description);
    }
}

using Tameru.SharedKernel.Results;

namespace Tameru.Ledger.Application;

/// <summary>Stable Ledger error codes (docs/ERROR_HANDLING.md, BUSINESS_RULES.md).</summary>
public static class LedgerErrors
{
    public static readonly Error TransactionNotFound = Error.NotFound("Transaction not found.");

    public static Error InvalidType(string value) =>
        Error.Validation($"'{value}' is not a valid transaction type.");

    public static Error InvalidStatus(string value) =>
        Error.Validation($"'{value}' is not a valid transaction status.");

    public static readonly Error AccountNotFound =
        new("account_not_found", "A referenced account does not exist or is inactive.");
}

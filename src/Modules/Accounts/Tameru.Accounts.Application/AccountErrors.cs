using Tameru.SharedKernel.Results;

namespace Tameru.Accounts.Application;

/// <summary>Stable Accounts error codes (docs/ERROR_HANDLING.md, BUSINESS_RULES.md).</summary>
public static class AccountErrors
{
    public static readonly Error AccountNotFound = Error.NotFound("Account not found.");

    public static readonly Error GroupNotFound = Error.NotFound("Account group not found.");

    public static readonly Error AccountInUse =
        new("account_in_use", "This account is referenced by transactions and cannot be deactivated.");

    public static Error InvalidType(string value) =>
        Error.Validation($"'{value}' is not a valid account type.");
}

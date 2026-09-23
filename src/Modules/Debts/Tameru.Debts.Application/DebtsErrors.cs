using Tameru.SharedKernel.Results;

namespace Tameru.Debts.Application;

public static class DebtsErrors
{
    public static readonly Error LiabilityNotFound = Error.NotFound("Liability not found.");
    public static readonly Error PaymentNotFound = Error.NotFound("Payment not found.");
    public static Error InvalidType(string value) => Error.Validation($"'{value}' is not a valid liability type.");
    public static Error InvalidStatus(string value) => Error.Validation($"'{value}' is not a valid liability status.");
}

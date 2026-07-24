namespace Tameru.SharedKernel.Domain;

/// <summary>
/// Thrown when a domain invariant / business rule is violated. Carries a stable machine
/// <see cref="Code"/> that the API maps to an HTTP status and error envelope
/// (see docs/ERROR_HANDLING.md and docs/BUSINESS_RULES.md).
/// </summary>
public sealed class DomainRuleException : Exception
{
    public DomainRuleException(string code, string message) : base(message) => Code = code;

    /// <summary>Stable snake_case rule code, e.g. <c>transfer_same_account</c>.</summary>
    public string Code { get; }
}

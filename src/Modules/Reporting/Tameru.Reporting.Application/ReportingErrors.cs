using Tameru.SharedKernel.Results;

namespace Tameru.Reporting.Application;

/// <summary>Stable Reporting validation error codes (docs/ERROR_HANDLING.md).</summary>
public static class ReportingErrors
{
    public static Error InvalidMonth(int value) =>
        Error.Validation($"'{value}' is not a valid month; expected 1..12.");

    public static Error InvalidGranularity(string value) =>
        Error.Validation($"'{value}' is not a valid granularity; expected 'daily' or 'monthly'.");

    public static readonly Error InvalidDateRange =
        Error.Validation("'from' must be on or before 'to'.");
}

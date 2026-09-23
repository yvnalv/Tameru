namespace Tameru.Debts.Domain;

public static class LiabilityType
{
    public const string Debt = "Debt";
    public const string Installment = "Installment";
    public const string Receivable = "Receivable";

    public static readonly string[] All = [Debt, Installment, Receivable];

    public static bool IsValid(string? value) =>
        value is not null && All.Contains(value, StringComparer.OrdinalIgnoreCase);
}

public static class LiabilityStatus
{
    public const string Active = "Active";
    public const string PaidOff = "PaidOff";
    public const string Defaulted = "Defaulted";

    public static readonly string[] All = [Active, PaidOff, Defaulted];

    public static bool IsValid(string? value) =>
        value is not null && All.Contains(value, StringComparer.OrdinalIgnoreCase);
}

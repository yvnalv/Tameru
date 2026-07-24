using System.Globalization;
using Tameru.SharedKernel.Domain;

namespace Tameru.SharedKernel.ValueObjects;

/// <summary>
/// Money: a decimal amount plus an explicit ISO-4217 currency code (CLAUDE.md: money always carries
/// a currency code; IDR is functional). Never use floating point for amounts. Arithmetic requires
/// matching currencies. Stored precision is 2 decimals (numeric(19,2)); see docs/DATABASE.md.
/// </summary>
public sealed class Money : ValueObject
{
    public const int Scale = 2;

    public const string FunctionalCurrency = "IDR";

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }

    /// <summary>ISO-4217 alphabetic code, upper-case (e.g. "IDR").</summary>
    public string Currency { get; }

    public bool IsZero => Amount == 0m;

    public static Money Create(decimal amount, string currency)
    {
        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
        {
            throw new ArgumentException("Currency must be a 3-letter ISO-4217 code.", nameof(currency));
        }

        return new Money(amount, currency.Trim().ToUpperInvariant());
    }

    public static Money Zero(string currency = FunctionalCurrency) => Create(0m, currency);

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount - other.Amount, Currency);
    }

    public Money Multiply(decimal factor) => new(Amount * factor, Currency);

    /// <summary>Rounds to <see cref="Scale"/> decimals using banker's rounding.</summary>
    public Money Round() => new(Math.Round(Amount, Scale, MidpointRounding.ToEven), Currency);

    public static Money operator +(Money left, Money right) => left.Add(right);

    public static Money operator -(Money left, Money right) => left.Subtract(right);

    public static Money operator *(Money money, decimal factor) => money.Multiply(factor);

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
        {
            throw new InvalidOperationException(
                $"Cannot operate on different currencies: {Currency} vs {other.Currency}.");
        }
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() =>
        $"{Amount.ToString("0.00", CultureInfo.InvariantCulture)} {Currency}";
}

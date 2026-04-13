using System;
using System.Collections.Generic;
using System.Text;

namespace OAI.Domain.ValueObjects;

public sealed class Money : IEquatable<Money>
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency is required.", nameof(currency));

        Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        Currency = currency.Trim().ToUpperInvariant();
    }

    public static Money Zero(string currency) => new(0m, currency);

    public bool IsCloseTo(Money other, decimal tolerance = 0.01m)
    {
        if (other is null) return false;
        EnsureSameCurrency(other);
        return Math.Abs(Amount - other.Amount) <= tolerance;
    }

    public static Money operator +(Money left, Money right)
    {
        left.EnsureSameCurrency(right);
        return new Money(left.Amount + right.Amount, left.Currency);
    }

    public static Money operator -(Money left, Money right)
    {
        left.EnsureSameCurrency(right);
        return new Money(left.Amount - right.Amount, left.Currency);
    }

    private void EnsureSameCurrency(Money other)
    {
        if (other is null)
            throw new ArgumentNullException(nameof(other));

        if (!string.Equals(Currency, other.Currency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Money currency mismatch.");
    }

    public bool Equals(Money? other)
    {
        if (other is null) return false;
        return Amount == other.Amount && Currency == other.Currency;
    }

    public override bool Equals(object? obj) => obj is Money other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Amount, Currency);

    public override string ToString() => $"{Amount:N2} {Currency}";
}

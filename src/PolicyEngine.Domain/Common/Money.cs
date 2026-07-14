namespace PolicyEngine.Domain.Common;

/// <summary>
/// Value object representing a monetary amount in a specific currency.
/// All amounts are rounded to 2 decimal places, away from zero,
/// mirroring how premiums are handled in policy administration systems.
/// </summary>
public readonly record struct Money(decimal Amount, string Currency)
{
    public static Money Zar(decimal amount) =>
        new(Math.Round(amount, 2, MidpointRounding.AwayFromZero), "ZAR");

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return this with { Amount = Amount + other.Amount };
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return this with { Amount = Amount - other.Amount };
    }

    public Money Multiply(decimal factor) =>
        this with { Amount = Math.Round(Amount * factor, 2, MidpointRounding.AwayFromZero) };

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
            throw new DomainException($"Cannot operate on {Currency} and {other.Currency}.");
    }

    public override string ToString() => $"{Currency} {Amount:N2}";
}

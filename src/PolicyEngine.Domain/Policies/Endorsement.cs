using PolicyEngine.Domain.Common;

namespace PolicyEngine.Domain.Policies;

/// <summary>A mid-term adjustment recorded against a policy.</summary>
public sealed class Endorsement
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public DateOnly EffectiveDate { get; private set; }
    public Money PreviousSumInsured { get; private set; }
    public Money NewSumInsured { get; private set; }
    public Money PremiumDelta { get; private set; }
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

    private Endorsement() { } // EF Core

    internal Endorsement(DateOnly effectiveDate, Money previousSumInsured, Money newSumInsured, Money premiumDelta)
    {
        EffectiveDate = effectiveDate;
        PreviousSumInsured = previousSumInsured;
        NewSumInsured = newSumInsured;
        PremiumDelta = premiumDelta;
    }
}

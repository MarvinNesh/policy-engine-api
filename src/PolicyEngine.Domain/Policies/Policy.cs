using PolicyEngine.Domain.Common;
using PolicyEngine.Domain.Pricing;

namespace PolicyEngine.Domain.Policies;

/// <summary>
/// Aggregate root for an insurance policy. A policy starts life as a quote and
/// moves through a strict lifecycle: Quoted -> Bound -> Active -> (Cancelled | Expired).
/// All state transitions and premium arithmetic are enforced here, so no caller
/// can put a policy into an invalid state.
/// </summary>
public sealed class Policy
{
    private readonly List<Endorsement> _endorsements = [];

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string? PolicyNumber { get; private set; }
    public string HolderName { get; private set; } = string.Empty;
    public int HolderAge { get; private set; }
    public ProductType Product { get; private set; }
    public PolicyStatus Status { get; private set; }
    public PolicyTerm Term { get; private set; }
    public Money SumInsured { get; private set; }
    public Money AnnualPremium { get; private set; }
    public DateOnly? CancelledOn { get; private set; }
    public Money? RefundDue { get; private set; }
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

    public IReadOnlyCollection<Endorsement> Endorsements => _endorsements.AsReadOnly();

    private Policy() { } // EF Core

    private Policy(string holderName, int holderAge, ProductType product,
                   PolicyTerm term, Money sumInsured, Money annualPremium)
    {
        HolderName = holderName;
        HolderAge = holderAge;
        Product = product;
        Term = term;
        SumInsured = sumInsured;
        AnnualPremium = annualPremium;
        Status = PolicyStatus.Quoted;
    }

    /// <summary>Creates a new quote with a premium calculated by the rating engine.</summary>
    public static Policy Quote(string holderName, int holderAge, ProductType product,
                               DateOnly coverStart, Money sumInsured, IPremiumCalculator calculator)
    {
        if (string.IsNullOrWhiteSpace(holderName))
            throw new DomainException("Holder name is required.");
        if (holderAge is < 18 or > 100)
            throw new DomainException("Holder age must be between 18 and 100.");
        if (sumInsured.Amount <= 0)
            throw new DomainException("Sum insured must be greater than zero.");

        var premium = calculator.CalculateAnnualPremium(
            new RatingInput(product, sumInsured, holderAge));

        return new Policy(holderName.Trim(), holderAge, product,
                          PolicyTerm.AnnualFrom(coverStart), sumInsured, premium);
    }

    /// <summary>Binds a quote, allocating a policy number. Quoted -> Bound.</summary>
    public void Bind()
    {
        EnsureStatus(PolicyStatus.Quoted, "Only a quote can be bound.");
        PolicyNumber = GeneratePolicyNumber();
        Status = PolicyStatus.Bound;
    }

    /// <summary>Puts a bound policy on risk. Bound -> Active.</summary>
    public void Activate()
    {
        EnsureStatus(PolicyStatus.Bound, "Only a bound policy can be activated.");
        Status = PolicyStatus.Active;
    }

    /// <summary>
    /// Applies a mid-term adjustment (change of sum insured). The premium delta is
    /// charged or refunded pro rata for the remaining days of the term.
    /// </summary>
    public Endorsement ApplyMidTermAdjustment(Money newSumInsured, DateOnly effectiveDate,
                                              IPremiumCalculator calculator)
    {
        EnsureStatus(PolicyStatus.Active, "Mid-term adjustments require an active policy.");
        if (newSumInsured.Amount <= 0)
            throw new DomainException("Sum insured must be greater than zero.");
        if (!Term.Contains(effectiveDate))
            throw new DomainException("Adjustment date must fall within the policy term.");

        var newAnnualPremium = calculator.CalculateAnnualPremium(
            new RatingInput(Product, newSumInsured, HolderAge));

        var proRataFactor = (decimal)Term.RemainingDaysFrom(effectiveDate) / Term.TotalDays;
        var premiumDelta = newAnnualPremium.Subtract(AnnualPremium).Multiply(proRataFactor);

        var endorsement = new Endorsement(effectiveDate, SumInsured, newSumInsured, premiumDelta);
        _endorsements.Add(endorsement);

        SumInsured = newSumInsured;
        AnnualPremium = newAnnualPremium;
        return endorsement;
    }

    /// <summary>Cancels an active policy, computing a pro-rata refund of unused premium.</summary>
    public Money Cancel(DateOnly effectiveDate)
    {
        EnsureStatus(PolicyStatus.Active, "Only an active policy can be cancelled.");
        if (!Term.Contains(effectiveDate))
            throw new DomainException("Cancellation date must fall within the policy term.");

        var proRataFactor = (decimal)Term.RemainingDaysFrom(effectiveDate) / Term.TotalDays;
        RefundDue = AnnualPremium.Multiply(proRataFactor);
        CancelledOn = effectiveDate;
        Status = PolicyStatus.Cancelled;
        return RefundDue.Value;
    }

    /// <summary>
    /// Renews the policy for a further annual term starting the day after expiry,
    /// re-rated at current rates. Returns the renewal policy, already bound and active.
    /// </summary>
    public Policy Renew(IPremiumCalculator calculator)
    {
        EnsureStatus(PolicyStatus.Active, "Only an active policy can be renewed.");

        var renewalStart = Term.EndDate.AddDays(1);
        var renewal = Quote(HolderName, HolderAge, Product, renewalStart, SumInsured, calculator);
        renewal.Bind();
        renewal.Activate();

        Status = PolicyStatus.Expired;
        return renewal;
    }

    private void EnsureStatus(PolicyStatus expected, string message)
    {
        if (Status != expected)
            throw new DomainException($"{message} Current status: {Status}.");
    }

    private static string GeneratePolicyNumber() =>
        $"POL-{DateTime.UtcNow:yyyyMM}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
}

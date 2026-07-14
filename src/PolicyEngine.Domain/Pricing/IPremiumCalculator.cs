using PolicyEngine.Domain.Common;

namespace PolicyEngine.Domain.Pricing;

/// <summary>Rating engine abstraction so pricing rules can evolve independently.</summary>
public interface IPremiumCalculator
{
    Money CalculateAnnualPremium(RatingInput input);
}

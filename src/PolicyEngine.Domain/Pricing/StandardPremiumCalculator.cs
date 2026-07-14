using PolicyEngine.Domain.Common;
using PolicyEngine.Domain.Policies;

namespace PolicyEngine.Domain.Pricing;

/// <summary>
/// Simple table-driven rating engine:
///   annual premium = sum insured x base rate x age loading, floored at a minimum premium.
/// Rates are illustrative but the structure (base rate + risk loadings + minimum
/// premium) reflects how short-term insurance products are actually rated.
/// </summary>
public sealed class StandardPremiumCalculator : IPremiumCalculator
{
    private static readonly Dictionary<ProductType, decimal> BaseAnnualRates = new()
    {
        [ProductType.Motor] = 0.045m,     // 4.5% of sum insured
        [ProductType.Household] = 0.012m, // 1.2%
        [ProductType.Device] = 0.080m     // 8.0%
    };

    private static readonly Money MinimumAnnualPremium = Money.Zar(600m);

    public Money CalculateAnnualPremium(RatingInput input)
    {
        if (!BaseAnnualRates.TryGetValue(input.Product, out var baseRate))
            throw new DomainException($"No rate on file for product '{input.Product}'.");

        var ageLoading = input.HolderAge switch
        {
            < 25 => 1.30m, // young-driver / high-risk loading
            > 65 => 1.15m,
            _ => 1.00m
        };

        var premium = input.SumInsured.Multiply(baseRate).Multiply(ageLoading);
        return premium.Amount < MinimumAnnualPremium.Amount ? MinimumAnnualPremium : premium;
    }
}

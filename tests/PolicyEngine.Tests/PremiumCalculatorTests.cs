using PolicyEngine.Domain.Common;
using PolicyEngine.Domain.Policies;
using PolicyEngine.Domain.Pricing;
using Xunit;

namespace PolicyEngine.Tests;

public class PremiumCalculatorTests
{
    private readonly StandardPremiumCalculator _calculator = new();

    [Theory]
    [InlineData(ProductType.Motor, 200_000, 35, 9_000)]      // 200k x 4.5%
    [InlineData(ProductType.Household, 500_000, 40, 6_000)]  // 500k x 1.2%
    [InlineData(ProductType.Device, 15_000, 30, 1_200)]      // 15k x 8%
    public void Calculates_base_premium_for_standard_risk(
        ProductType product, decimal sumInsured, int age, decimal expected)
    {
        var premium = _calculator.CalculateAnnualPremium(
            new RatingInput(product, Money.Zar(sumInsured), age));

        Assert.Equal(expected, premium.Amount);
        Assert.Equal("ZAR", premium.Currency);
    }

    [Fact]
    public void Applies_young_driver_loading()
    {
        var premium = _calculator.CalculateAnnualPremium(
            new RatingInput(ProductType.Motor, Money.Zar(200_000), 22));

        Assert.Equal(11_700m, premium.Amount); // 9 000 x 1.30
    }

    [Fact]
    public void Applies_senior_loading()
    {
        var premium = _calculator.CalculateAnnualPremium(
            new RatingInput(ProductType.Motor, Money.Zar(200_000), 70));

        Assert.Equal(10_350m, premium.Amount); // 9 000 x 1.15
    }

    [Fact]
    public void Enforces_minimum_premium()
    {
        var premium = _calculator.CalculateAnnualPremium(
            new RatingInput(ProductType.Household, Money.Zar(10_000), 40)); // would be 120

        Assert.Equal(600m, premium.Amount);
    }
}

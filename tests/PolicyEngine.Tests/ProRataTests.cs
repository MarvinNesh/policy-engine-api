using PolicyEngine.Domain.Common;
using PolicyEngine.Domain.Policies;
using PolicyEngine.Domain.Pricing;
using Xunit;

namespace PolicyEngine.Tests;

public class ProRataTests
{
    private readonly StandardPremiumCalculator _calculator = new();
    private static readonly DateOnly Start = new(2026, 1, 1); // 365-day term ends 2026-12-31

    private Policy NewActivePolicy()
    {
        var policy = Policy.Quote("Sipho Dlamini", 40, ProductType.Motor,
                                  Start, Money.Zar(200_000), _calculator); // premium 9 000
        policy.Bind();
        policy.Activate();
        return policy;
    }

    [Fact]
    public void Mid_term_increase_charges_pro_rata_delta()
    {
        var policy = NewActivePolicy();

        // Increase sum insured from 200k (premium 9 000) to 300k (premium 13 500)
        // effective 2026-07-01: 184 days remain of 365.
        var endorsement = policy.ApplyMidTermAdjustment(
            Money.Zar(300_000), new DateOnly(2026, 7, 1), _calculator);

        var expected = Math.Round(4_500m * 184m / 365m, 2, MidpointRounding.AwayFromZero);
        Assert.Equal(expected, endorsement.PremiumDelta.Amount);
        Assert.Equal(13_500m, policy.AnnualPremium.Amount);
        Assert.Equal(300_000m, policy.SumInsured.Amount);
        Assert.Single(policy.Endorsements);
    }

    [Fact]
    public void Mid_term_decrease_produces_negative_delta()
    {
        var policy = NewActivePolicy();

        var endorsement = policy.ApplyMidTermAdjustment(
            Money.Zar(100_000), new DateOnly(2026, 7, 1), _calculator);

        Assert.True(endorsement.PremiumDelta.Amount < 0);
    }

    [Fact]
    public void Cancellation_refunds_unused_premium_pro_rata()
    {
        var policy = NewActivePolicy();

        // Cancel effective 2026-07-01: 184 of 365 days unused.
        var refund = policy.Cancel(new DateOnly(2026, 7, 1));

        var expected = Math.Round(9_000m * 184m / 365m, 2, MidpointRounding.AwayFromZero);
        Assert.Equal(expected, refund.Amount);
        Assert.Equal(PolicyStatus.Cancelled, policy.Status);
        Assert.Equal(new DateOnly(2026, 7, 1), policy.CancelledOn);
    }

    [Fact]
    public void Cancellation_on_last_day_refunds_one_day()
    {
        var policy = NewActivePolicy();

        var refund = policy.Cancel(new DateOnly(2026, 12, 31));

        var expected = Math.Round(9_000m / 365m, 2, MidpointRounding.AwayFromZero);
        Assert.Equal(expected, refund.Amount);
    }
}

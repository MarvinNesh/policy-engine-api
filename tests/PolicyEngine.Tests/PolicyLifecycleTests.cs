using PolicyEngine.Domain.Common;
using PolicyEngine.Domain.Policies;
using PolicyEngine.Domain.Pricing;
using Xunit;

namespace PolicyEngine.Tests;

public class PolicyLifecycleTests
{
    private readonly StandardPremiumCalculator _calculator = new();
    private static readonly DateOnly Start = new(2026, 1, 1);

    private Policy NewActivePolicy()
    {
        var policy = Policy.Quote("Thandi Mokoena", 35, ProductType.Motor,
                                  Start, Money.Zar(200_000), _calculator);
        policy.Bind();
        policy.Activate();
        return policy;
    }

    [Fact]
    public void Quote_starts_in_quoted_status_without_policy_number()
    {
        var quote = Policy.Quote("Thandi Mokoena", 35, ProductType.Motor,
                                 Start, Money.Zar(200_000), _calculator);

        Assert.Equal(PolicyStatus.Quoted, quote.Status);
        Assert.Null(quote.PolicyNumber);
        Assert.Equal(9_000m, quote.AnnualPremium.Amount);
    }

    [Fact]
    public void Binding_allocates_policy_number()
    {
        var policy = NewActivePolicy();

        Assert.Equal(PolicyStatus.Active, policy.Status);
        Assert.StartsWith("POL-", policy.PolicyNumber);
    }

    [Fact]
    public void Cannot_bind_twice()
    {
        var policy = NewActivePolicy();
        var ex = Assert.Throws<DomainException>(policy.Bind);
        Assert.Contains("Only a quote can be bound", ex.Message);
    }

    [Fact]
    public void Cannot_adjust_a_quote()
    {
        var quote = Policy.Quote("Thandi Mokoena", 35, ProductType.Motor,
                                 Start, Money.Zar(200_000), _calculator);

        Assert.Throws<DomainException>(() =>
            quote.ApplyMidTermAdjustment(Money.Zar(250_000), Start.AddMonths(3), _calculator));
    }

    [Fact]
    public void Cannot_cancel_outside_term()
    {
        var policy = NewActivePolicy();
        Assert.Throws<DomainException>(() => policy.Cancel(Start.AddYears(2)));
    }

    [Fact]
    public void Renewal_expires_original_and_creates_active_follow_on_term()
    {
        var policy = NewActivePolicy();
        var renewal = policy.Renew(_calculator);

        Assert.Equal(PolicyStatus.Expired, policy.Status);
        Assert.Equal(PolicyStatus.Active, renewal.Status);
        Assert.Equal(policy.Term.EndDate.AddDays(1), renewal.Term.StartDate);
        Assert.NotEqual(policy.PolicyNumber, renewal.PolicyNumber);
    }

    [Fact]
    public void Rejects_underage_holder()
    {
        Assert.Throws<DomainException>(() =>
            Policy.Quote("Too Young", 17, ProductType.Device, Start, Money.Zar(10_000), _calculator));
    }
}

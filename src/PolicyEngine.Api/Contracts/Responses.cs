using PolicyEngine.Domain.Policies;

namespace PolicyEngine.Api.Contracts;

public sealed record MoneyDto(decimal Amount, string Currency);

public sealed record EndorsementDto(
    Guid Id, DateOnly EffectiveDate, MoneyDto PreviousSumInsured,
    MoneyDto NewSumInsured, MoneyDto PremiumDelta, DateTime CreatedAtUtc);

public sealed record PolicyDto(
    Guid Id, string? PolicyNumber, string HolderName, int HolderAge,
    string Product, string Status, DateOnly CoverStart, DateOnly CoverEnd,
    MoneyDto SumInsured, MoneyDto AnnualPremium,
    DateOnly? CancelledOn, MoneyDto? RefundDue,
    IReadOnlyList<EndorsementDto> Endorsements)
{
    public static PolicyDto From(Policy p) => new(
        p.Id, p.PolicyNumber, p.HolderName, p.HolderAge,
        p.Product.ToString(), p.Status.ToString(),
        p.Term.StartDate, p.Term.EndDate,
        new MoneyDto(p.SumInsured.Amount, p.SumInsured.Currency),
        new MoneyDto(p.AnnualPremium.Amount, p.AnnualPremium.Currency),
        p.CancelledOn,
        p.RefundDue is { } r ? new MoneyDto(r.Amount, r.Currency) : null,
        p.Endorsements.Select(e => new EndorsementDto(
            e.Id, e.EffectiveDate,
            new MoneyDto(e.PreviousSumInsured.Amount, e.PreviousSumInsured.Currency),
            new MoneyDto(e.NewSumInsured.Amount, e.NewSumInsured.Currency),
            new MoneyDto(e.PremiumDelta.Amount, e.PremiumDelta.Currency),
            e.CreatedAtUtc)).ToList());
}

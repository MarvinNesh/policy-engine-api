using PolicyEngine.Domain.Common;
using PolicyEngine.Domain.Policies;

namespace PolicyEngine.Domain.Pricing;

/// <summary>The factors used to rate a risk.</summary>
public readonly record struct RatingInput(ProductType Product, Money SumInsured, int HolderAge);

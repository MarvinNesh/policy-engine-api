using System.ComponentModel.DataAnnotations;

namespace PolicyEngine.Api.Contracts;

public sealed record CreateQuoteRequest(
    [Required, StringLength(120, MinimumLength = 2)] string HolderName,
    [Range(18, 100)] int HolderAge,
    [Required] string Product,
    [Range(typeof(decimal), "1", "100000000")] decimal SumInsured,
    DateOnly? CoverStart);

public sealed record MidTermAdjustmentRequest(
    [Range(typeof(decimal), "1", "100000000")] decimal NewSumInsured,
    [Required] DateOnly EffectiveDate);

public sealed record CancelPolicyRequest([Required] DateOnly EffectiveDate);

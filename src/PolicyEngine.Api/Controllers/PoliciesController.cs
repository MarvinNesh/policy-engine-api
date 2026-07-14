using Microsoft.AspNetCore.Mvc;
using PolicyEngine.Api.Contracts;
using PolicyEngine.Domain.Common;
using PolicyEngine.Domain.Policies;
using PolicyEngine.Domain.Pricing;

namespace PolicyEngine.Api.Controllers;

[ApiController]
[Route("api/policies")]
[Produces("application/json")]
public sealed class PoliciesController(IPolicyRepository repository, IPremiumCalculator calculator)
    : ControllerBase
{
    /// <summary>Lists policies, optionally filtered by status.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PolicyDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] PolicyStatus? status, CancellationToken ct)
    {
        var policies = await repository.ListAsync(status, ct);
        return Ok(policies.Select(PolicyDto.From).ToList());
    }

    /// <summary>Fetches a single policy (or quote) by id.</summary>
    [HttpGet("{id:guid}", Name = "GetPolicy")]
    [ProducesResponseType(typeof(PolicyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var policy = await repository.GetAsync(id, ct);
        return policy is null ? NotFound() : Ok(PolicyDto.From(policy));
    }

    /// <summary>Applies a mid-term adjustment with a pro-rata premium delta.</summary>
    [HttpPost("{id:guid}/adjustments")]
    [ProducesResponseType(typeof(PolicyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Adjust(Guid id, MidTermAdjustmentRequest request, CancellationToken ct)
    {
        var policy = await repository.GetAsync(id, ct);
        if (policy is null) return NotFound();

        policy.ApplyMidTermAdjustment(Money.Zar(request.NewSumInsured), request.EffectiveDate, calculator);
        await repository.UpdateAsync(policy, ct);
        return Ok(PolicyDto.From(policy));
    }

    /// <summary>Cancels an active policy and reports the pro-rata refund due.</summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(PolicyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Cancel(Guid id, CancelPolicyRequest request, CancellationToken ct)
    {
        var policy = await repository.GetAsync(id, ct);
        if (policy is null) return NotFound();

        policy.Cancel(request.EffectiveDate);
        await repository.UpdateAsync(policy, ct);
        return Ok(PolicyDto.From(policy));
    }

    /// <summary>Renews an active policy for a further annual term at current rates.</summary>
    [HttpPost("{id:guid}/renew")]
    [ProducesResponseType(typeof(PolicyDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Renew(Guid id, CancellationToken ct)
    {
        var policy = await repository.GetAsync(id, ct);
        if (policy is null) return NotFound();

        var renewal = policy.Renew(calculator);
        await repository.UpdateAsync(policy, ct);
        await repository.AddAsync(renewal, ct);
        return CreatedAtRoute("GetPolicy", new { id = renewal.Id }, PolicyDto.From(renewal));
    }
}

using Microsoft.AspNetCore.Mvc;
using PolicyEngine.Api.Contracts;
using PolicyEngine.Domain.Common;
using PolicyEngine.Domain.Policies;
using PolicyEngine.Domain.Pricing;

namespace PolicyEngine.Api.Controllers;

[ApiController]
[Route("api/quotes")]
[Produces("application/json")]
public sealed class QuotesController(IPolicyRepository repository, IPremiumCalculator calculator)
    : ControllerBase
{
    /// <summary>Creates a quote and returns the calculated annual premium.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(PolicyDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateQuote(CreateQuoteRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<ProductType>(request.Product, ignoreCase: true, out var product))
            return BadRequest(new { detail = $"Unknown product '{request.Product}'. Valid: Motor, Household, Device." });

        var coverStart = request.CoverStart ?? DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var quote = Policy.Quote(request.HolderName, request.HolderAge, product,
                                 coverStart, Money.Zar(request.SumInsured), calculator);

        await repository.AddAsync(quote, ct);
        return CreatedAtRoute("GetPolicy", new { id = quote.Id }, PolicyDto.From(quote));
    }

    /// <summary>Binds a quote into a policy and puts it on risk.</summary>
    [HttpPost("{id:guid}/bind")]
    [ProducesResponseType(typeof(PolicyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> BindQuote(Guid id, CancellationToken ct)
    {
        var quote = await repository.GetAsync(id, ct);
        if (quote is null) return NotFound();

        quote.Bind();
        quote.Activate();
        await repository.UpdateAsync(quote, ct);
        return Ok(PolicyDto.From(quote));
    }
}

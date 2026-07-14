using PolicyEngine.Domain.Common;

namespace PolicyEngine.Api.Middleware;

/// <summary>
/// Translates domain rule violations into RFC 7807 problem responses (422)
/// so controllers stay free of try/catch noise.
/// </summary>
public sealed class DomainExceptionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (DomainException ex)
        {
            context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://httpstatuses.com/422",
                title = "Business rule violation",
                status = 422,
                detail = ex.Message
            });
        }
    }
}

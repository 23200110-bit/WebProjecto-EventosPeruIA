using EventosPeruIA.CORE.Core.DTOs.Recommendations;
using EventosPeruIA.CORE.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventosPeruIA.API.Controllers;

[ApiController]
[Authorize]
[Route("api/recommendations")]
public sealed class RecommendationController(IRecommendationService service) : AuthenticatedControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<RecommendationResponse>>> Get(
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await service.GetForUserAsync(userId, limit, cancellationToken));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }
}

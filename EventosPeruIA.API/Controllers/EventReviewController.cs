using EventosPeruIA.CORE.Core.DTOs.EventReviews;
using EventosPeruIA.CORE.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventosPeruIA.API.Controllers;

[ApiController]
[Route("api/events/{eventId:int}/reviews")]
public sealed class EventReviewController(IEventReviewService service) : AuthenticatedControllerBase
{
    [HttpGet]
    public async Task<ActionResult<EventReviewSummaryResponse>> Get(
        int eventId,
        CancellationToken cancellationToken)
    {
        return Ok(await service.GetPublicSummaryAsync(eventId, cancellationToken));
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<EventReviewResponse>> Create(
        int eventId,
        CreateEventReviewRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var review = await service.CreateAsync(userId, eventId, request, cancellationToken);
            return CreatedAtAction(nameof(Get), new { eventId }, review);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }
}

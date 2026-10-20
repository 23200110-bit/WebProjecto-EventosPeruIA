using EventosPeruIA.CORE.Core.DTOs.EventReviews;

namespace EventosPeruIA.CORE.Core.Interfaces.Services;

public interface IEventReviewService
{
    Task<EventReviewResponse> CreateAsync(int userId, int eventId, CreateEventReviewRequest request, CancellationToken cancellationToken = default);
    Task<EventReviewSummaryResponse> GetPublicSummaryAsync(int eventId, CancellationToken cancellationToken = default);
}

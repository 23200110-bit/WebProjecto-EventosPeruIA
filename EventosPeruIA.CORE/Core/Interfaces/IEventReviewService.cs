using EventosPeruIA.CORE.Core.DTOs;

namespace EventosPeruIA.CORE.Core.Interfaces;

public interface IEventReviewService
{
    Task<EventReviewResponse> CreateAsync(int userId, int eventId, CreateEventReviewRequest request, CancellationToken cancellationToken = default);
    Task<EventReviewSummaryResponse> GetPublicSummaryAsync(int eventId, CancellationToken cancellationToken = default);
}

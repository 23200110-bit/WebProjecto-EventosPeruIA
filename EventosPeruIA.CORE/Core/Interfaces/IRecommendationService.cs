using EventosPeruIA.CORE.Core.DTOs;

namespace EventosPeruIA.CORE.Core.Interfaces;

public interface IRecommendationService
{
    Task<IReadOnlyCollection<RecommendationResponse>> GetForUserAsync(int userId, int limit = 20, CancellationToken cancellationToken = default);
}

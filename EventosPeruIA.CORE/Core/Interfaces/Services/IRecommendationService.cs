using EventosPeruIA.CORE.Core.DTOs.Recommendations;

namespace EventosPeruIA.CORE.Core.Interfaces.Services;

public interface IRecommendationService
{
    Task<IReadOnlyCollection<RecommendationResponse>> GetForUserAsync(int userId, int limit = 20, CancellationToken cancellationToken = default);
}

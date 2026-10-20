using EventosPeruIA.CORE.Core.Entities;

namespace EventosPeruIA.CORE.Core.Interfaces.Repositories;

public interface IRecommendationRepository
{
    Task<Usuarios?> GetUserWithSignalsAsync(int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Eventos>> GetAvailableEventsAsync(CancellationToken cancellationToken = default);
}

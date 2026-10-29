using EventosPeruIA.CORE.Core.Entities;

namespace EventosPeruIA.CORE.Core.Interfaces;

public interface IEventReviewRepository
{
    Task<Entradas?> GetUsedTicketAsync(Guid ticketId, int userId, CancellationToken cancellationToken = default);
    Task<bool> HasActiveReviewAsync(int userId, int eventId, CancellationToken cancellationToken = default);
    Task AddAsync(Resenas review, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<VwResenasEvento>> GetPublicReviewsAsync(int eventId, CancellationToken cancellationToken = default);
    Task<VwPromedioResenasEvento?> GetSummaryAsync(int eventId, CancellationToken cancellationToken = default);
}

using EventosPeruIA.CORE.Core.Entities;
using EventosPeruIA.CORE.Core.Interfaces.Repositories;
using EventosPeruIA.CORE.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EventosPeruIA.CORE.Infrastructure.Repositories;

public sealed class EventReviewRepository(PostgresContext context) : IEventReviewRepository
{
    public Task<Entradas?> GetUsedTicketAsync(
        Guid ticketId,
        int userId,
        CancellationToken cancellationToken = default) =>
        context.Entradas
            .Include(ticket => ticket.Evento)
            .AsNoTracking()
            .SingleOrDefaultAsync(ticket =>
                ticket.EntradaId == ticketId &&
                ticket.CompradorId == userId &&
                ticket.Estado == "Utilizada", cancellationToken);

    public Task<bool> HasActiveReviewAsync(
        int userId,
        int eventId,
        CancellationToken cancellationToken = default) =>
        context.Resenas.AnyAsync(review =>
            review.UsuarioId == userId &&
            review.EventoId == eventId &&
            review.Estado == "Activa", cancellationToken);

    public async Task AddAsync(Resenas review, CancellationToken cancellationToken = default)
    {
        await context.Resenas.AddAsync(review, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<VwResenasEvento>> GetPublicReviewsAsync(
        int eventId,
        CancellationToken cancellationToken = default) =>
        await context.VwResenasEvento
            .AsNoTracking()
            .Where(review => review.EventoId == eventId)
            .OrderByDescending(review => review.FechaCreacion)
            .ToListAsync(cancellationToken);

    public Task<VwPromedioResenasEvento?> GetSummaryAsync(
        int eventId,
        CancellationToken cancellationToken = default) =>
        context.VwPromedioResenasEvento
            .AsNoTracking()
            .SingleOrDefaultAsync(summary => summary.EventoId == eventId, cancellationToken);
}

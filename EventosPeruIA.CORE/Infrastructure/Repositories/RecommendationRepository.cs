using EventosPeruIA.CORE.Core.Entities;
using EventosPeruIA.CORE.Core.Interfaces;
using EventosPeruIA.CORE.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EventosPeruIA.CORE.Infrastructure.Repositories;

public sealed class RecommendationRepository(PostgresContext context) : IRecommendationRepository
{
    public Task<Usuarios?> GetUserWithSignalsAsync(int userId, CancellationToken cancellationToken = default) =>
        context.Usuarios
            .Include(user => user.UsuarioIntereses)
            .ThenInclude(interest => interest.Categoria)
            .Include(user => user.UsuarioEventoInteraccion)
            .ThenInclude(interaction => interaction.Evento)
            .ThenInclude(evento => evento!.EventosEmbeddings)
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.UsuarioId == userId, cancellationToken);

    public async Task<IReadOnlyCollection<Eventos>> GetAvailableEventsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await context.Eventos
            .Include(evento => evento.Categoria)
            .Include(evento => evento.EventosEmbeddings)
            .Include(evento => evento.Entradas)
            .AsNoTracking()
            .Where(evento =>
                evento.Estado == "Publicado" &&
                evento.EstadoModeracion == "Aprobado" &&
                evento.FechaInicio > now &&
                evento.Entradas.Count(ticket => ticket.Estado != "Anulada") < evento.Aforo)
            .ToListAsync(cancellationToken);
    }
}

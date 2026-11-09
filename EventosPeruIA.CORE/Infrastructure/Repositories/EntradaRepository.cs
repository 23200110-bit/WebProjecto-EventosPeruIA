using EventosPeruIA.CORE.Core.Entities;
using EventosPeruIA.CORE.Core.Interfaces;
using EventosPeruIA.CORE.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EventosPeruIA.CORE.Infrastructure.Repositories
{
    public class EntradaRepository : IEntradaRepository
    {
        private readonly PostgresContext _context;

        public EntradaRepository(PostgresContext context)
        {
            _context = context;
        }

        public async Task<List<Entradas>> GetEntradasByUserIdAsync(int userId)
        {
            return await _context.Entradas
                .AsNoTracking()
                .Include(entrada => entrada.Evento)
                    .ThenInclude(evento => evento.EventosCancelacion)
                .Include(entrada => entrada.Comprador)
                .Where(entrada => entrada.CompradorId == userId)
                .OrderByDescending(entrada => entrada.FechaEmision)
                .ToListAsync();
        }

        public async Task<Entradas?> GetEntradaByIdAsync(Guid entradaId)
        {
            return await _context.Entradas
                .AsNoTracking()
                .Include(entrada => entrada.Evento)
                    .ThenInclude(evento => evento.EventosCancelacion)
                .Include(entrada => entrada.Comprador)
                .SingleOrDefaultAsync(entrada => entrada.EntradaId == entradaId);
        }
    }
}
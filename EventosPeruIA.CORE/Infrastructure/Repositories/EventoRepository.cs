using EventosPeruIA.CORE.Core.Entities;
using EventosPeruIA.CORE.Core.Interfaces;
using EventosPeruIA.CORE.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EventosPeruIA.CORE.Infrastructure.Repositories
{
    public class EventoRepository : IEventoRepository
    {
        private readonly PostgresContext _context;

        public EventoRepository(PostgresContext context)
        {
            _context = context;
        }

        public async Task<List<Eventos>> ListarEventosPublicadosAsync(int? categoriaId, string? ubicacion, DateTime? fecha)
        {
            var query = _context.Eventos
                .Include(e => e.Categoria)
                .Include(e => e.Organizador)
                .AsNoTracking()
                .Where(e => e.Estado == "Publicado" && e.EstadoModeracion == "Aprobado" && e.Organizador.Estado == "Activo");

            if (categoriaId.HasValue)
                query = query.Where(e => e.CategoriaId == categoriaId.Value);

            if (!string.IsNullOrWhiteSpace(ubicacion))
                query = query.Where(e => e.Direccion.Contains(ubicacion) || e.Ciudad.Contains(ubicacion));

            if (fecha.HasValue)
            {
                var fechaUtc = DateTime.SpecifyKind(fecha.Value.Date, DateTimeKind.Utc);
                query = query.Where(e => e.FechaInicio.Date == fechaUtc);
            }

            return await query.ToListAsync();
        }

        public async Task<Eventos?> GetEventoByIdAsync(int id)
        {
            return await _context.Eventos
                .Include(e => e.Categoria)
                .Include(e => e.Organizador)
                .FirstOrDefaultAsync(e => e.EventoId == id);
        }

        public async Task<int> CreateEventoAsync(Eventos evento)
        {
            await _context.Eventos.AddAsync(evento);
            await _context.SaveChangesAsync();
            return evento.EventoId;
        }

        public async Task<bool> UpdateEventoAsync(Eventos evento)
        {
            _context.Eventos.Update(evento);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> CategoriaExistsAsync(int categoriaId)
        {
            return await _context.Categorias.AnyAsync(c => c.CategoriaId == categoriaId && c.Activo);
        }

        public async Task<bool> MotivoCancelacionExistsAsync(int motivoCancelacionId)
        {
            return await _context.MotivosCancelacion.AnyAsync(m => m.MotivoCancelacionId == motivoCancelacionId);
        }

        public async Task<int> CountEntradasActivasAsync(int eventoId)
        {
            return await _context.Entradas
                .CountAsync(e => e.EventoId == eventoId && (e.Estado == "Valida" || e.Estado == "Utilizada"));
        }

        public async Task InactivarEmbeddingsVigentesAsync(int eventoId)
        {
            var embeddings = await _context
                .EventosEmbeddings.Where(x => x.EventoId == eventoId && x.Vigente)
                .ToListAsync();
            foreach (var embedding in embeddings)
            {
                embedding.Vigente = false;
            }
            await _context.SaveChangesAsync();
        }

        public async Task CrearNotificacionesCompradoresAsync(int eventoId, string tipo, string mensaje)
        {
            var compradores = await _context.Entradas
                .Where(x => x.EventoId == eventoId && x.Estado == "Valida")
                .Select(x => x.CompradorId)
                .Distinct()
                .ToListAsync();

            var notificaciones = compradores.Select(cId => new Notificaciones
            {
                UsuarioId = cId,
                EventoId = eventoId,
                Tipo = tipo,
                Mensaje = mensaje
            }).ToList();

            if (notificaciones.Any())
            {
                await _context.Notificaciones.AddRangeAsync(notificaciones);
                await _context.SaveChangesAsync();
            }
        }

        public async Task CancelarEntradasYMarcarReembolsoAsync(int eventoId)
        {
            var entradas = await _context.Entradas
                .Where(e => e.EventoId == eventoId && e.Estado == "Valida")
                .ToListAsync();
            foreach (var entrada in entradas)
            {
                entrada.Estado = "Anulada";
                entrada.EstadoReembolso = "PendienteReembolso";
            }
            await _context.SaveChangesAsync();
        }
    }
}
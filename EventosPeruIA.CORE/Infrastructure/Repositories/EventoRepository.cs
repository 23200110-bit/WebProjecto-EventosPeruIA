using EventosPeruIA.CORE.Core.Entities;
using EventosPeruIA.CORE.Core.Interfaces;
using EventosPeruIA.CORE.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EventosPeruIA.CORE.Infrastructure.Repositories
{
    public class EventoRepository : IEventoRepository
    {
        private readonly PostgresContext _dbContext;

        public EventoRepository(PostgresContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Eventos?> GetEventoById(int id)
        {
            return await _dbContext.Eventos
                .FirstOrDefaultAsync(e => e.EventoId == id);
        }

        public async Task<int> CreateEvento(Eventos evento)
        {
            await _dbContext.Eventos.AddAsync(evento);
            var rows = await _dbContext.SaveChangesAsync();
            return rows > 0 ? evento.EventoId : 0;
        }

        public async Task<bool> UpdateEvento(Eventos evento)
        {
            var existing = await _dbContext.Eventos
                .FirstOrDefaultAsync(e => e.EventoId == evento.EventoId);

            if (existing == null) return false;

            var cambioProgramacion = existing.FechaInicio != evento.FechaInicio
                || existing.FechaFin != evento.FechaFin
                || existing.Direccion != evento.Direccion;

            var cambioContenido = cambioProgramacion
                || existing.Nombre != evento.Nombre
                || existing.Descripcion != evento.Descripcion
                || existing.CategoriaId != evento.CategoriaId
                || existing.Precio != evento.Precio;

            existing.CategoriaId = evento.CategoriaId;
            existing.Nombre = evento.Nombre;
            existing.Descripcion = evento.Descripcion;
            existing.FechaInicio = evento.FechaInicio;
            existing.FechaFin = evento.FechaFin;
            existing.Direccion = evento.Direccion;
            existing.Latitud = evento.Latitud;
            existing.Longitud = evento.Longitud;
            existing.Aforo = evento.Aforo;
            existing.Precio = evento.Precio;
            existing.Estado = evento.Estado;
            existing.FechaPublicacion = evento.FechaPublicacion;
            existing.FechaActualizacion = DateTime.UtcNow;

            if (cambioContenido)
            {
                var embeddings = await _dbContext.EventosEmbeddings
                    .Where(x => x.EventoId == existing.EventoId && x.Vigente)
                    .ToListAsync();

                foreach (var embedding in embeddings)
                {
                    embedding.Vigente = false;
                }
            }

            if (cambioProgramacion)
            {
                var compradores = await _dbContext.Entradas
                    .Where(x => x.EventoId == existing.EventoId && x.Estado == "Valida")
                    .Select(x => x.CompradorId)
                    .Distinct()
                    .ToListAsync();

                foreach (var compradorId in compradores)
                {
                    await _dbContext.Notificaciones.AddAsync(new Notificaciones
                    {
                        UsuarioId = compradorId,
                        EventoId = existing.EventoId,
                        Tipo = "CambioEvento",
                        Mensaje = $"El evento \"{existing.Nombre}\" tiene cambios en su fecha, hora o ubicación. Revisa los nuevos detalles."
                    });
                }
            }

            try
            {
                var rows = await _dbContext.SaveChangesAsync();
                return rows > 0;
            }
            catch (DbUpdateConcurrencyException)
            {
                return false;
            }
        }

        public async Task<bool> CancelEvento(int id, int userId, int motivoCancelacionId, string descripcionAdicional)
        {
            try
            {
                await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT fn_cancelar_evento({id}, {userId}, {motivoCancelacionId}, {descripcionAdicional})");
                return true;
            }
            catch (PostgresException)
            {
                return false;
            }
        }

        public async Task<bool> CategoriaExists(int categoriaId)
        {
            return await _dbContext.Categorias
                .AnyAsync(c => c.CategoriaId == categoriaId && c.Activo);
        }

        public async Task<bool> MotivoCancelacionExists(int motivoCancelacionId)
        {
            return await _dbContext.MotivosCancelacion
                .AnyAsync(m => m.MotivoCancelacionId == motivoCancelacionId);
        }

        public async Task<int> CountEntradasActivas(int eventoId)
        {
            return await _dbContext.Entradas
                .CountAsync(e => e.EventoId == eventoId
                              && (e.Estado == "Valida" || e.Estado == "Utilizada"));
        }
    }
}
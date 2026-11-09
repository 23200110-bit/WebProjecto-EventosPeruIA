using EventosPeruIA.CORE.Core.DTOs.Reportes;
using EventosPeruIA.CORE.Core.Interfaces;
using EventosPeruIA.CORE.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EventosPeruIA.CORE.Infrastructure.Repositories
{
    public class ReporteRepository : IReporteRepository
    {
        private readonly PostgresContext _context;

        public ReporteRepository(PostgresContext context)
        {
            _context = context;
        }

        public async Task<bool> EsOrganizadorDelEventoAsync(int organizadorId, int eventoId)
        {
            return await _context.Eventos
                .AsNoTracking()
                .AnyAsync(e => e.OrganizadorId == organizadorId && e.EventoId == eventoId);
        }

        public async Task<List<DatosVentaEventoRaw>> GetVentasEventoRawAsync(int organizadorId, int? eventoId)
        {
            var query = _context.VwVentasPorEvento
                .AsNoTracking()
                .Where(v => v.OrganizadorId == organizadorId);

            if (eventoId.HasValue)
            {
                query = query.Where(v => v.EventoId == eventoId.Value);
            }

            return await query
                .Where(v => v.EventoId.HasValue)
                .Select(v => new DatosVentaEventoRaw
                {
                    EventoId = v.EventoId!.Value,
                    NombreEvento = v.NombreEvento ?? string.Empty,
                    EntradasDisponibles = v.EntradasDisponibles ?? 0
                })
                .ToListAsync();
        }

        public async Task<List<DatosAsistenciaRaw>> GetAsistentesRawAsync(int organizadorId, int? eventoId, DateTime? fechaInicio, DateTime? fechaFin)
        {
            var query = _context.VwAsistentesEvento
                .AsNoTracking()
                .Where(a => a.OrganizadorId == organizadorId);

            if (eventoId.HasValue)
                query = query.Where(a => a.EventoId == eventoId.Value);

            // FIX: Convertir a UTC para compatibilidad con timestamptz en PostgreSQL
            if (fechaInicio.HasValue)
            {
                var inicioUtc = DateTime.SpecifyKind(fechaInicio.Value, DateTimeKind.Utc);
                query = query.Where(a => a.FechaEmision >= inicioUtc);
            }

            if (fechaFin.HasValue)
            {
                var finUtc = DateTime.SpecifyKind(fechaFin.Value, DateTimeKind.Utc);
                query = query.Where(a => a.FechaEmision <= finUtc);
            }

            return await query
                .Where(a => a.EventoId.HasValue)
                .Select(a => new DatosAsistenciaRaw
                {
                    EventoId = a.EventoId!.Value,
                    Estado = a.Estado ?? string.Empty,
                    FechaEmision = a.FechaEmision ?? DateTime.MinValue
                })
                .ToListAsync();
        }

        public async Task<List<DatosVentaDiariaRaw>> GetVentasDiariasRawAsync(int organizadorId, int? eventoId, DateTime? fechaInicio, DateTime? fechaFin)
        {
            var query = _context.VwVentasPorFecha
                .AsNoTracking()
                .Where(v => v.OrganizadorId == organizadorId);

            if (eventoId.HasValue)
                query = query.Where(v => v.EventoId == eventoId.Value);

            if (fechaInicio.HasValue)
            {
                var startDate = DateOnly.FromDateTime(fechaInicio.Value);
                query = query.Where(v => v.Fecha >= startDate);
            }

            if (fechaFin.HasValue)
            {
                var endDate = DateOnly.FromDateTime(fechaFin.Value);
                query = query.Where(v => v.Fecha <= endDate);
            }

            return await query
                .Where(v => v.EventoId.HasValue && v.Fecha.HasValue)
                .Select(v => new DatosVentaDiariaRaw
                {
                    EventoId = v.EventoId!.Value,
                    Fecha = v.Fecha!.Value,
                    EntradasVendidas = v.EntradasVendidasEseDia ?? 0,
                    Ingresos = v.IngresosEseDia ?? 0m
                })
                .ToListAsync();
        }

        public async Task<List<DatosReembolsoRaw>> GetReembolsosRawAsync(int organizadorId, int? eventoId, DateTime? fechaInicio, DateTime? fechaFin)
        {
            var query = _context.Entradas
                .AsNoTracking()
                .Where(e => e.Evento.OrganizadorId == organizadorId && e.EstadoReembolso == "PendienteReembolso");

            if (eventoId.HasValue)
                query = query.Where(e => e.EventoId == eventoId.Value);

            // FIX: Convertir a UTC para compatibilidad con timestamptz en PostgreSQL
            if (fechaInicio.HasValue)
            {
                var inicioUtc = DateTime.SpecifyKind(fechaInicio.Value, DateTimeKind.Utc);
                query = query.Where(e => e.FechaEmision >= inicioUtc);
            }

            if (fechaFin.HasValue)
            {
                var finUtc = DateTime.SpecifyKind(fechaFin.Value, DateTimeKind.Utc);
                query = query.Where(e => e.FechaEmision <= finUtc);
            }

            return await query.Select(e => new DatosReembolsoRaw
            {
                EventoId = e.EventoId,
                PrecioPagado = e.PrecioPagado,
                FechaEmision = e.FechaEmision
            }).ToListAsync();
        }
    }
}
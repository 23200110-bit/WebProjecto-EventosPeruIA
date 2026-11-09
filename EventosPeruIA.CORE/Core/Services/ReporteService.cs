using System.ComponentModel.DataAnnotations;
using System.Text;
using EventosPeruIA.CORE.Core.DTOs.Reportes;
using EventosPeruIA.CORE.Core.Interfaces;

namespace EventosPeruIA.CORE.Core.Services
{
    public class ReporteService : IReporteService
    {
        private readonly IReporteRepository _reporteRepository;

        public ReporteService(IReporteRepository reporteRepository)
        {
            _reporteRepository = reporteRepository;
        }

        public async Task<ReporteDashboardOrganizadorDto> GetReporteDashboardOrganizadorAsync(int organizadorId, ReporteFiltroRequestDto filtro)
        {
            if (filtro.FechaInicio.HasValue && filtro.FechaFin.HasValue && filtro.FechaInicio.Value > filtro.FechaFin.Value)
            {
                throw new ValidationException("La fecha de inicio no puede ser posterior a la fecha de fin.");
            }

            if (filtro.EventoId.HasValue)
            {
                bool esDuenio = await _reporteRepository.EsOrganizadorDelEventoAsync(organizadorId, filtro.EventoId.Value);
                if (!esDuenio)
                {
                    throw new UnauthorizedAccessException("Acceso denegado: No tiene permisos para consultar datos de este evento.");
                }
            }

            var eventosRaw = await _reporteRepository.GetVentasEventoRawAsync(organizadorId, filtro.EventoId);
            var asistentesRaw = await _reporteRepository.GetAsistentesRawAsync(organizadorId, filtro.EventoId, filtro.FechaInicio, filtro.FechaFin);
            var ventasDiariasRaw = await _reporteRepository.GetVentasDiariasRawAsync(organizadorId, filtro.EventoId, filtro.FechaInicio, filtro.FechaFin);
            var reembolsosRaw = await _reporteRepository.GetReembolsosRawAsync(organizadorId, filtro.EventoId, filtro.FechaInicio, filtro.FechaFin);

            var resumenes = eventosRaw.Select(evt =>
            {
                var ticketsEvento = asistentesRaw.Where(a => a.EventoId == evt.EventoId).ToList();
                var ventasEvento = ventasDiariasRaw.Where(v => v.EventoId == evt.EventoId).ToList();
                var reembolsosEvento = reembolsosRaw.Where(r => r.EventoId == evt.EventoId).ToList();

                int validas = ticketsEvento.Count(t => string.Equals(t.Estado, "Valida", StringComparison.OrdinalIgnoreCase));
                int utilizadas = ticketsEvento.Count(t => string.Equals(t.Estado, "Utilizada", StringComparison.OrdinalIgnoreCase));
                int anuladas = ticketsEvento.Count(t => string.Equals(t.Estado, "Anulada", StringComparison.OrdinalIgnoreCase));

                return new ResumenVentasEventoDto
                {
                    EventoId = evt.EventoId,
                    NombreEvento = evt.NombreEvento,
                    EntradasValidasVendidas = validas,
                    EntradasUtilizadas = utilizadas,
                    CapacidadDisponible = evt.EntradasDisponibles,
                    IngresosTotalesSoles = ventasEvento.Sum(v => v.Ingresos),
                    EntradasAnuladasCount = anuladas,
                    MontoPendienteReembolsoSoles = reembolsosEvento.Sum(r => r.PrecioPagado)
                };
            }).ToList();

            var evolucionDiaria = ventasDiariasRaw
                .GroupBy(v => v.Fecha)
                .OrderBy(g => g.Key)
                .Select(g => new GraficoVentasDiariasDto
                {
                    Fecha = g.Key.ToDateTime(TimeOnly.MinValue),
                    EntradasVendidas = g.Sum(x => x.EntradasVendidas),
                    MontoTotalSoles = g.Sum(x => x.Ingresos)
                })
                .ToList();

            bool tieneDatos = resumenes.Any() || evolucionDiaria.Any();

            return new ReporteDashboardOrganizadorDto
            {
                FechaGeneracion = DateTime.UtcNow,
                FiltrosAplicados = filtro,
                ResumenEventos = resumenes,
                EvolucionVentasDiarias = evolucionDiaria,
                GranTotalIngresosSoles = resumenes.Sum(r => r.IngresosTotalesSoles),
                GranTotalEntradasActivas = resumenes.Sum(r => r.TotalEntradasActivas),
                TieneDatos = tieneDatos,
                MensajeInformativo = tieneDatos ? string.Empty : "No existen ventas para el filtro elegido."
            };
        }

        public async Task<ArchivoExportadoResponseDto> ExportarReporteExcelAsync(int organizadorId, ReporteFiltroRequestDto filtro)
        {
            var reporte = await GetReporteDashboardOrganizadorAsync(organizadorId, filtro);

            var sb = new StringBuilder();
            sb.AppendLine("Reporte de Ventas por Evento");
            sb.AppendLine($"Fecha de Generación: {reporte.FechaGeneracion:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();
            sb.AppendLine("Evento,Entradas Válidas,Entradas Utilizadas,Capacidad Disponible,Ingresos (S/)");

            foreach (var item in reporte.ResumenEventos)
            {
                sb.AppendLine($"\"{item.NombreEvento}\",{item.EntradasValidasVendidas},{item.EntradasUtilizadas},{item.CapacidadDisponible},{item.IngresosTotalesSoles}");
            }

            return new ArchivoExportadoResponseDto
            {
                NombreArchivo = $"Reporte_Ventas_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv",
                ContentType = "text/csv",
                ContenidoBytes = Encoding.UTF8.GetBytes(sb.ToString())
            };
        }

        public async Task<ArchivoExportadoResponseDto> ExportarReportePdfAsync(int organizadorId, ReporteFiltroRequestDto filtro)
        {
            var reporte = await GetReporteDashboardOrganizadorAsync(organizadorId, filtro);

            var sb = new StringBuilder();
            sb.AppendLine("=========================================");
            sb.AppendLine("         REPORTE DE VENTAS (PDF)        ");
            sb.AppendLine("=========================================");
            sb.AppendLine($"Fecha de Generación: {reporte.FechaGeneracion:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Total Recaudado: S/ {reporte.GranTotalIngresosSoles}");
            sb.AppendLine($"Total Entradas Activas: {reporte.GranTotalEntradasActivas}");
            sb.AppendLine("=========================================");

            return new ArchivoExportadoResponseDto
            {
                NombreArchivo = $"Reporte_Ventas_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf",
                ContentType = "application/pdf",
                ContenidoBytes = Encoding.UTF8.GetBytes(sb.ToString())
            };
        }
    }
}
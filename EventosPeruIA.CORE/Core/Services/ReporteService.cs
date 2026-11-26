using System.ComponentModel.DataAnnotations;
using EventosPeruIA.CORE.Core.DTOs.Reportes;
using EventosPeruIA.CORE.Core.Interfaces;
using EventosPeruIA.CORE.Infrastructure.Reportes;

namespace EventosPeruIA.CORE.Core.Services;

public class ReporteService(IReporteRepository reporteRepository) : IReporteService
{
    public async Task<ReporteDashboardOrganizadorDto> GetReporteDashboardOrganizadorAsync(
        int organizadorId, ReporteFiltroRequestDto filtro)
    {
        if (filtro.EventoId is <= 0)
            throw new ValidationException("El identificador del evento debe ser mayor que cero.");

        if (filtro.FechaInicio is { } inicio && filtro.FechaFin is { } fin && inicio > fin)
            throw new ValidationException("La fecha de inicio no puede ser posterior a la fecha de fin.");

        var datos = await reporteRepository.ObtenerDatosAsync(organizadorId, filtro);
        if (filtro.EventoId.HasValue && datos.Eventos.Count == 0)
            throw new UnauthorizedAccessException(
                "No tienes permisos para consultar datos de este evento.");

        var ventasPorEvento = datos.VentasDiarias.ToLookup(venta => venta.EventoId);
        var resumenes = datos.Eventos.Select(evento =>
        {
            var ventas = ventasPorEvento[evento.EventoId];
            var ventasPEN = ventas.Where(venta => venta.Moneda == "PEN");
            var ventasUSD = ventas.Where(venta => venta.Moneda == "USD");

            return new ResumenVentasEventoDto
            {
                EventoId = evento.EventoId,
                NombreEvento = evento.NombreEvento,
                EstadoEvento = evento.EstadoEvento,
                EntradasValidasVendidas = ventas.Sum(venta => venta.EntradasValidas),
                EntradasUtilizadas = ventas.Sum(venta => venta.EntradasUtilizadas),
                CapacidadDisponible = evento.EstadoEvento is "Cancelado" or "Finalizado"
                    ? 0
                    : Math.Max(0, evento.EntradasDisponibles),
                IngresosTotalesSoles = ventasPEN.Sum(venta => venta.Ingresos),
                IngresosTotalesUsd = ventasUSD.Sum(venta => venta.Ingresos),
                EntradasAnuladasCount = ventas.Sum(venta => venta.EntradasAnuladas),
                EntradasPendientesReembolso = ventas.Sum(venta => venta.EntradasPendientesReembolso),
                MontoPendienteReembolsoSoles = ventasPEN.Sum(venta => venta.MontoPendienteReembolso),
                MontoPendienteReembolsoUsd = ventasUSD.Sum(venta => venta.MontoPendienteReembolso)
            };
        }).ToList();

        var evolucionDiaria = datos.VentasDiarias
            .Where(venta => venta.EntradasVendidas > 0)
            .GroupBy(venta => venta.Fecha)
            .OrderBy(grupo => grupo.Key)
            .Select(grupo => new GraficoVentasDiariasDto
            {
                Fecha = grupo.Key,
                EntradasVendidas = grupo.Sum(venta => venta.EntradasVendidas),
                MontoTotalSoles = grupo.Where(venta => venta.Moneda == "PEN").Sum(venta => venta.Ingresos),
                MontoTotalUsd = grupo.Where(venta => venta.Moneda == "USD").Sum(venta => venta.Ingresos)
            })
            .ToList();

        var tieneDatos = evolucionDiaria.Count > 0;
        return new ReporteDashboardOrganizadorDto
        {
            FechaGeneracion = DateTime.UtcNow,
            FiltrosAplicados = new ReporteFiltroRequestDto
            {
                EventoId = filtro.EventoId,
                FechaInicio = filtro.FechaInicio,
                FechaFin = filtro.FechaFin
            },
            ResumenEventos = resumenes,
            EvolucionVentasDiarias = evolucionDiaria,
            GranTotalIngresosSoles = resumenes.Sum(resumen => resumen.IngresosTotalesSoles),
            GranTotalIngresosUsd = resumenes.Sum(resumen => resumen.IngresosTotalesUsd),
            GranTotalEntradasActivas = resumenes.Sum(resumen => resumen.TotalEntradasActivas),
            TieneDatos = tieneDatos,
            MensajeInformativo = tieneDatos ? string.Empty : "No existen ventas para el filtro elegido."
        };
    }

    public async Task<ArchivoExportadoResponseDto> ExportarReporteExcelAsync(
        int organizadorId, ReporteFiltroRequestDto filtro) =>
        ReporteExportador.CrearExcel(await GetReporteDashboardOrganizadorAsync(organizadorId, filtro));

    public async Task<ArchivoExportadoResponseDto> ExportarReportePdfAsync(
        int organizadorId, ReporteFiltroRequestDto filtro) =>
        ReporteExportador.CrearPdf(await GetReporteDashboardOrganizadorAsync(organizadorId, filtro));
}
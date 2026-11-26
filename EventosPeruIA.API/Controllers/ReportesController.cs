using System.ComponentModel.DataAnnotations;
using EventosPeruIA.API.Services;
using EventosPeruIA.CORE.Core.DTOs.Reportes;
using EventosPeruIA.CORE.Core.Interfaces;
using EventosPeruIA.CORE.Infrastructure.Reportes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventosPeruIA.API.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
public class ReportesController : ControllerBase
{
    private readonly IReporteService reporteService;
    private readonly ReporteOrganizadorCache cache;

    public ReportesController(IReporteService reporteService, ReporteOrganizadorCache cache)
    {
        this.reporteService = reporteService;
        this.cache = cache;
    }

    [HttpGet("dashboard-organizador")]
    public async Task<IActionResult> GetDashboard([FromQuery] ReporteFiltroRequestDto filtro)
    {
        try
        {
            var organizadorId = GetUserIdFromClaims();
            var reporte = await reporteService.GetReporteDashboardOrganizadorAsync(organizadorId, filtro);
            cache.Guardar(organizadorId, reporte);
            return Ok(reporte);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
    }

    [HttpGet("exportar/excel")]
    public Task<IActionResult> ExportarExcel(
        [FromQuery] ReporteFiltroRequestDto filtro, [FromQuery] Guid? reporteId) =>
        Exportar(filtro, reporteId, reporteService.ExportarReporteExcelAsync, ReporteExportador.CrearExcel);

    [HttpGet("exportar/pdf")]
    public Task<IActionResult> ExportarPdf(
        [FromQuery] ReporteFiltroRequestDto filtro, [FromQuery] Guid? reporteId) =>
        Exportar(filtro, reporteId, reporteService.ExportarReportePdfAsync, ReporteExportador.CrearPdf);

    private async Task<IActionResult> Exportar(
        ReporteFiltroRequestDto filtro,
        Guid? reporteId,
        Func<int, ReporteFiltroRequestDto, Task<ArchivoExportadoResponseDto>> generar,
        Func<ReporteDashboardOrganizadorDto, ArchivoExportadoResponseDto> exportar)
    {
        try
        {
            var organizadorId = GetUserIdFromClaims();
            ArchivoExportadoResponseDto archivo;
            if (reporteId is Guid id)
            {
                var reporte = cache.Obtener(organizadorId, id);
                if (reporte is null)
                    return NotFound(new
                    {
                        message = "El informe ya no está disponible. Vuelve a aplicar los filtros."
                    });

                archivo = exportar(reporte);
            }
            else
            {
                archivo = await generar(organizadorId, filtro);
            }

            return File(archivo.ContenidoBytes, archivo.ContentType, archivo.NombreArchivo);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
    }

    private int GetUserIdFromClaims()
    {
        if (!int.TryParse(User.FindFirst("UserId")?.Value, out var userId))
            throw new UnauthorizedAccessException("Usuario no autenticado o token inválido.");

        return userId;
    }
}
using System.ComponentModel.DataAnnotations;
using EventosPeruIA.CORE.Core.DTOs.Reportes;
using EventosPeruIA.CORE.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventosPeruIA.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ReportesController : ControllerBase
    {
        private readonly IReporteService _reporteService;

        public ReportesController(IReporteService reporteService)
        {
            _reporteService = reporteService;
        }

        [HttpGet("dashboard-organizador")]
        public async Task<IActionResult> GetDashboard([FromQuery] ReporteFiltroRequestDto filtro)
        {
            try
            {
                int organizadorId = GetUserIdFromClaims();
                var reporte = await _reporteService.GetReporteDashboardOrganizadorAsync(organizadorId, filtro);
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
        public async Task<IActionResult> ExportarExcel([FromQuery] ReporteFiltroRequestDto filtro)
        {
            try
            {
                int organizadorId = GetUserIdFromClaims();
                var archivo = await _reporteService.ExportarReporteExcelAsync(organizadorId, filtro);
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

        [HttpGet("exportar/pdf")]
        public async Task<IActionResult> ExportarPdf([FromQuery] ReporteFiltroRequestDto filtro)
        {
            try
            {
                int organizadorId = GetUserIdFromClaims();
                var archivo = await _reporteService.ExportarReportePdfAsync(organizadorId, filtro);
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
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("Usuario no autenticado o token inválido.");
            }
            return userId;
        }
    }
}
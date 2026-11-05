using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using EventosPeruIA.CORE.Core.DTOs.Reports;
using EventosPeruIA.CORE.Core.Interfaces;
using EventosPeruIA.CORE.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventosPeruIA.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpGet("organizer-dashboard")]
        public async Task<IActionResult> GetDashboard([FromQuery] ReportFilterRequestDto filter)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var organizerId))
                {
                    return Unauthorized(new { message = "Usuario no autenticado." });
                }

                var dashboard = await _reportService.GetOrganizerDashboardReportAsync(organizerId, filter);
                return Ok(dashboard);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al procesar la solicitud del panel de control.", details = ex.Message });
            }
        }

        [HttpGet("export/excel")]
        public async Task<IActionResult> ExportExcel([FromQuery] ReportFilterRequestDto filter)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var organizerId))
                {
                    return Unauthorized(new { message = "Usuario no autenticado." });
                }

                var file = await _reportService.ExportReportToExcelAsync(organizerId, filter);
                return File(file.FileBytes, file.ContentType, file.FileName);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al exportar a Excel.", details = ex.Message });
            }
        }

        [HttpGet("export/pdf")]
        public async Task<IActionResult> ExportPdf([FromQuery] ReportFilterRequestDto filter)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var organizerId))
                {
                    return Unauthorized(new { message = "Usuario no autenticado." });
                }

                var file = await _reportService.ExportReportToPdfAsync(organizerId, filter);
                return File(file.FileBytes, file.ContentType, file.FileName);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al exportar a PDF.", details = ex.Message });
            }
        }
    }
}
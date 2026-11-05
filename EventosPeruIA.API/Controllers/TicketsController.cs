using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using EventosPeruIA.CORE.Core.DTOs;
using EventosPeruIA.CORE.Core.Interfaces;
using EventosPeruIA.CORE.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EventosPeruIA.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TicketsController : ControllerBase
    {
        private readonly ITicketService _ticketService;

        public TicketsController(ITicketService ticketService)
        {
            _ticketService = ticketService;
        }

        [HttpGet("my-tickets")]
        public async Task<IActionResult> GetMyTickets()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Usuario no autenticado." });
                }

                var tickets = await _ticketService.GetUserTicketsAsync(userId);
                return Ok(tickets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno al recuperar las entradas.", details = ex.Message });
            }
        }

        [HttpGet("{id:guid}/voucher")]
        public async Task<IActionResult> GetVoucher(Guid id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Usuario no autenticado." });
                }

                var voucher = await _ticketService.GetVoucherByTicketIdAsync(id, userId);
                return Ok(voucher);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (ValidationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno al obtener el voucher de la entrada.", details = ex.Message });
            }
        }
    }
}
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using EventosPeruIA.CORE.Core.DTOs.Eventos;
using EventosPeruIA.CORE.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventosPeruIA.API.Controllers
{
    [ApiController]
    [Route("api/eventos")]
    [Authorize]
    public class EventosController : ControllerBase
    {
        private readonly IEventoService _eventoService;

        public EventosController(IEventoService eventoService)
        {
            _eventoService = eventoService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Listar([FromQuery] EventoFiltroQueryDto filtro)
        {
            var eventos = await _eventoService.ListarEventosAsync(filtro);
            return Ok(eventos);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> ObtenerDetalle(int id)
        {
            var detalle = await _eventoService.ObtenerDetalleEventoAsync(id);
            if (detalle == null)
            {
                return NotFound(new { message = $"El evento con ID {id} no fue encontrado o no está disponible." });
            }
            return Ok(detalle);
        }

        [HttpPost]
        public async Task<IActionResult> CreateEvento([FromBody] EventoCreateDto dto)
        {
            try
            {
                int userId = GetUserId();
                int eventoId = await _eventoService.CreateEventoAsync(userId, dto);
                return Created($"/api/eventos/{eventoId}", new { eventoId });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvento(int id, [FromBody] EventoUpdateDto dto)
        {
            try
            {
                if (id != dto.EventoId)
                    return BadRequest(new { message = "El ID de la URL no coincide con el cuerpo de la petición." });

                int userId = GetUserId();
                await _eventoService.UpdateEventoAsync(userId, dto);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPatch("{id}/cancel")]
        public async Task<IActionResult> CancelEvento(int id, [FromBody] EventoCancelDto dto)
        {
            try
            {
                int userId = GetUserId();
                await _eventoService.CancelEventoAsync(id, userId, dto);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> PatchEvento(int id, [FromBody] EventoPatchDto dto)
        {
            try
            {
                int userId = GetUserId();
                await _eventoService.PatchEventoAsync(id, userId, dto);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
        }

        private int GetUserId()
        {
            var claim = User.FindFirst("UserId") ?? User.FindFirst("UsuarioId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null || !int.TryParse(claim.Value, out var userId))
            {
                throw new UnauthorizedAccessException("Usuario no autenticado o token inválido.");
            }
            return userId;
        }
    }
}
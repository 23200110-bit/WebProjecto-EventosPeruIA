using System.Security.Claims;
using EventosPeruIA.CORE.Core.DTOs;
using EventosPeruIA.CORE.Core.Interfaces;
using EventosPeruIA.CORE.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventosPeruIA.API.Controllers
{
    [ApiController]
    [Route("api/events")]
    [Authorize]
    public class EventsController : ControllerBase
    {
        private readonly IEventoService _eventoService;
        private readonly PostgresContext _context;

        public EventsController(
            IEventoService eventoService,
            PostgresContext context)
        {
            _eventoService = eventoService;
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Listar(
            [FromQuery] int? categoriaId,
            [FromQuery] string? ubicacion,
            [FromQuery] DateTime? fecha)
        {
            var query = _context.Eventos
                .Include(e => e.Categoria)
                .Where(e => e.Estado == "Publicado")
                .AsQueryable();

            if (categoriaId.HasValue)
            {
                query = query.Where(e => e.CategoriaId == categoriaId.Value);
            }

            if (!string.IsNullOrWhiteSpace(ubicacion))
            {
                query = query.Where(e => e.Direccion.Contains(ubicacion));
            }

            if (fecha.HasValue)
            {
                var fechaUtc = DateTime.SpecifyKind(fecha.Value.Date, DateTimeKind.Utc);
                query = query.Where(e => e.FechaInicio.Date == fechaUtc);
            }

            var eventos = await query.Select(e => new
            {
                e.EventoId,
                e.Nombre,
                e.Descripcion,
                e.FechaInicio,
                e.FechaFin,
                e.Direccion,
                e.Precio,
                e.Aforo,
                e.Estado,
                Categoria = e.Categoria != null ? e.Categoria.Nombre : null
            }).ToListAsync();

            return Ok(eventos);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> ObtenerDetalle(int id)
        {
            var evento = await _context.Eventos
                .Include(e => e.Categoria)
                .FirstOrDefaultAsync(e => e.EventoId == id);

            if (evento == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = $"El evento con ID {id} no fue encontrado."
                });
            }

            var ticketsVendidos = await _context.Entradas
                .CountAsync(t =>
                    t.EventoId == id &&
                    (t.Estado == "Valida" || t.Estado == "Utilizada"));

            var cuposDisponibles = Math.Max(0, evento.Aforo - ticketsVendidos);

            var detalle = new
            {
                evento.EventoId,
                evento.Nombre,
                evento.Descripcion,
                evento.FechaInicio,
                evento.FechaFin,
                evento.Direccion,
                evento.Precio,
                evento.Aforo,
                evento.Estado,
                Categoria = evento.Categoria != null ? evento.Categoria.Nombre : null,
                TicketsVendidos = ticketsVendidos,
                CuposDisponibles = cuposDisponibles,
                Agotado = cuposDisponibles == 0
            };

            return Ok(detalle);
        }

        [HttpPost]
        public async Task<IActionResult> CreateEvento([FromBody] EventoCreateDTO evento)
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var eventoId = await _eventoService.CreateEvento(userId, evento);

            if (eventoId == 0)
            {
                return BadRequest("No se pudo crear el evento. Verifica la categoría y que las fechas sean futuras y coherentes.");
            }

            return Created($"/api/events/{eventoId}", new { eventoId });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvento([FromBody] EventoUpdateDTO evento, int id)
        {
            if (id != evento.EventoId)
                return BadRequest();

            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var existing = await _eventoService.GetEventoById(id);

            if (existing == null) return NotFound();
            if (existing.OrganizadorId != userId) return Forbid();

            var result = await _eventoService.UpdateEvento(evento);

            if (!result)
            {
                return BadRequest("No se pudo actualizar el evento. Verifica su estado, la categoría, las fechas y que el aforo no sea menor a las entradas vendidas.");
            }

            return NoContent();
        }

        [HttpPatch("{id}/cancel")]
        public async Task<IActionResult> CancelEvento([FromBody] EventoCancelDTO evento, int id)
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var existing = await _eventoService.GetEventoById(id);

            if (existing == null) return NotFound();
            if (existing.OrganizadorId != userId) return Forbid();

            var result = await _eventoService.CancelEvento(id, userId, evento);

            if (!result)
            {
                return BadRequest("No se pudo cancelar el evento. Verifica que esté en Borrador o Publicado, que no haya iniciado y que el motivo sea válido.");
            }

            return NoContent();
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> PatchEvento(int id, [FromBody] EventoPatchDTO evento)
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var result = await _eventoService.PatchEvento(id, userId, evento);

            if (!result)
            {
                return NotFound(new { mensaje = "El evento no existe o no tienes permisos para modificarlo." });
            }

            return NoContent();
        }

        private int GetUserId()
        {
            var claim = User.FindFirst("UsuarioId")
                ?? User.FindFirst(ClaimTypes.NameIdentifier);

            if (claim == null) return 0;

            return int.TryParse(claim.Value, out var id) ? id : 0;
        }
    }
}
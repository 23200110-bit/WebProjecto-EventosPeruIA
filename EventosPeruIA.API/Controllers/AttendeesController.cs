using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using EventosPeruIA.CORE.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventosPeruIA.API.Controllers;

[ApiController, Authorize, Route("api/events/{id:int}/asistentes")]
public sealed class AttendeesController(IAsistentesService service) : AuthenticatedControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(int id, [FromQuery, StringLength(100)] string? buscar, [FromQuery] string? estado,
        [FromQuery, Range(1, 100000)] int pagina = 1, [FromQuery, Range(1, 100)] int tamanoPagina = 20, CancellationToken ct = default)
    {
        if (!TryGetUserId(out var usuarioId)) return Unauthorized();
        return Ok(await service.Listar(usuarioId, id, buscar, estado, pagina, tamanoPagina, ct));
    }
}

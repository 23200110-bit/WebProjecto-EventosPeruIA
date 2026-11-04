using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using EventosPeruIA.CORE.Core.DTOs;
using EventosPeruIA.CORE.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventosPeruIA.API.Controllers;

[ApiController, Route("api/admin"), Authorize(Roles = "Admin")]
public sealed class AdminController(IAdminService service) : AuthenticatedControllerBase
{
    [HttpGet("usuarios")]
    public async Task<IActionResult> Usuarios([FromQuery, StringLength(150)] string? buscar, [FromQuery] string? estado,
        [FromQuery, Range(1, 100000)] int pagina = 1, [FromQuery, Range(1, 100)] int tamanoPagina = 20, CancellationToken ct = default) =>
        Ok(await service.Usuarios(buscar, estado, pagina, tamanoPagina, ct));

    [HttpGet("eventos")]
    public async Task<IActionResult> Eventos([FromQuery, StringLength(150)] string? buscar, [FromQuery] string? estado,
        [FromQuery, Range(1, 100000)] int pagina = 1, [FromQuery, Range(1, 100)] int tamanoPagina = 20, CancellationToken ct = default) =>
        Ok(await service.Eventos(buscar, estado, pagina, tamanoPagina, ct));

    [HttpPatch("usuarios/{id:int}/estado")]
    public async Task<IActionResult> Estado(int id, [FromBody] CambiarEstadoUsuarioDTO dto, CancellationToken ct)
    {
        if (!TryGetUserId(out var admin)) return Unauthorized();
        await service.EstadoUsuario(admin, id, dto, ct);
        return NoContent();
    }
    [HttpPatch("eventos/{id:int}/moderar")]
    public async Task<IActionResult> Moderar(int id, [FromBody] ModerarEventoDTO dto, CancellationToken ct)
    {
        if (!TryGetUserId(out var admin)) return Unauthorized();
        await service.ModerarEvento(admin, id, dto, ct);
        return NoContent();
    }
}

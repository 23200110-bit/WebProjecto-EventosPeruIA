using EventosPeruIA.CORE.Core.DTOs;
using EventosPeruIA.CORE.Core.Entities;

namespace EventosPeruIA.CORE.Core.Interfaces;

public interface IAdminService
{
    Task<PaginaDTO<UsuarioAdminDTO>> Usuarios(string? buscar, string? estado, int pagina, int tamano, CancellationToken ct);
    Task<PaginaDTO<EventoAdminDTO>> Eventos(string? buscar, string? estado, int pagina, int tamano, CancellationToken ct);
    Task EstadoUsuario(int administradorId, int id, CambiarEstadoUsuarioDTO dto, CancellationToken ct);
    Task ModerarEvento(int administradorId, int id, ModerarEventoDTO dto, CancellationToken ct);
}

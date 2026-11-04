using EventosPeruIA.CORE.Core.DTOs;
using EventosPeruIA.CORE.Core.Entities;

namespace EventosPeruIA.CORE.Core.Interfaces;

public interface IAdminRepository
{
    string EstadoRetirado { get; }
    Task<PaginaDTO<UsuarioAdminDTO>> ListarUsuarios(string? buscar, string? estado, int pagina, int tamano, CancellationToken ct);
    Task<PaginaDTO<EventoAdminDTO>> ListarEventos(string? buscar, string? estado, int pagina, int tamano, CancellationToken ct);
    Task<Usuarios?> Usuario(int id, CancellationToken ct);
    Task<Eventos?> Evento(int id, CancellationToken ct);
    Task Guardar(AccionesModeracion accion, CancellationToken ct);
}

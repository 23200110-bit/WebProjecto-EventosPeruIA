using EventosPeruIA.CORE.Core.DTOs;
using EventosPeruIA.CORE.Core.Entities;

namespace EventosPeruIA.CORE.Core.Interfaces;

public interface IAsistentesRepository
{
    Task<Eventos?> Evento(int id, CancellationToken ct);
    Task<AsistentesResponseDTO> Listar(int eventoId, string? buscar, string? estado, int pagina, int tamano, CancellationToken ct);
}

using EventosPeruIA.CORE.Core.DTOs;
using EventosPeruIA.CORE.Core.Entities;

namespace EventosPeruIA.CORE.Core.Interfaces;

public interface IAsistentesService
{
    Task<AsistentesResponseDTO> Listar(int organizadorId, int eventoId, string? buscar, string? estado, int pagina, int tamano, CancellationToken ct);
}

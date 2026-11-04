using EventosPeruIA.CORE.Core.DTOs;
using EventosPeruIA.CORE.Core.Interfaces;

namespace EventosPeruIA.CORE.Core.Services;

public sealed class AsistentesService(IAsistentesRepository repository) : IAsistentesService
{
    public async Task<AsistentesResponseDTO> Listar(int organizadorId, int eventoId, string? buscar, string? estado, int pagina, int tamano, CancellationToken ct)
    {
        var evento = await repository.Evento(eventoId, ct)
            ?? throw new ProyectoException(404, "Evento no encontrado.");
        if (evento.OrganizadorId != organizadorId) throw new ProyectoException(403, "Solo puedes consultar asistentes de tus eventos.");
        if (estado != null && estado is not ("Valida" or "Utilizada" or "Anulada")) throw new ProyectoException(400, "Estado de entrada inválido.");
        return await repository.Listar(eventoId, buscar, estado, pagina, tamano, ct);
    }
}

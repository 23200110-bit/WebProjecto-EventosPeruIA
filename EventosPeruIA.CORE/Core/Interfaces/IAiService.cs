using EventosPeruIA.CORE.Core.DTOs;
using EventosPeruIA.CORE.Core.Entities;

namespace EventosPeruIA.CORE.Core.Interfaces;

public interface IAiService
{
    Task<ChatResponseDTO> Chat(int usuarioId, ChatRequestDTO request, CancellationToken ct);
}

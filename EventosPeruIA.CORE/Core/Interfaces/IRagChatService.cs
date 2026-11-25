using EventosPeruIA.CORE.Core.DTOs;

namespace EventosPeruIA.CORE.Core.Interfaces;

public interface IRagChatService
{
    Task<RagChatResponseDTO> Chat(string message, CancellationToken ct);
}

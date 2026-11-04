using EventosPeruIA.CORE.Core.DTOs;
using EventosPeruIA.CORE.Core.Entities;

namespace EventosPeruIA.CORE.Core.Interfaces;

public interface IAiRepository
{
    Task<string> TextoEmbedding(EventoChatDTO evento, CancellationToken ct);
    Task<List<EventoChatDTO>> EventosDisponibles(DateTime ahora, CancellationToken ct);
    Task<ConversacionesChatbot?> Conversacion(int id, int usuarioId, CancellationToken ct);
    Task<List<MensajesChatbot>> Historial(int id, CancellationToken ct);
    Task<List<EventosEmbeddings>> Embeddings(int[] ids, string modelo, CancellationToken ct);
    void AgregarEmbedding(EventosEmbeddings embedding);
    Task GuardarConversacion(ConversacionesChatbot conversacion, CancellationToken ct);
}

using EventosPeruIA.CORE.Core.DTOs;
using EventosPeruIA.CORE.Core.Interfaces;

namespace EventosPeruIA.CORE.Core.Services;

public sealed class RagChatService(IAiRepository repository, IModeloIA model) : IRagChatService
{
    public async Task<RagChatResponseDTO> Chat(string message, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ProyectoException(400, "Escribe una pregunta.");

        var queryEmbedding = await model.Embed(message.Trim(), "RETRIEVAL_QUERY", ct);
        var matches = await repository.MatchDocuments(queryEmbedding, 0.75, 5, ct);
        var context = matches.Select(match => $"[Fuente {match.Id}: {match.Title}]\n{match.Content}").ToArray();
        var reply = await model.ResponderRag(message.Trim(), context, ct);
        if (string.IsNullOrWhiteSpace(reply) || reply.Length > 12000)
            throw new ProyectoException(502, "El modelo devolvió una respuesta inválida. Intenta nuevamente.");

        return new(reply, matches.Select(match => new RagSourceDTO(match.Id, match.Title, match.Similarity)).ToArray());
    }
}

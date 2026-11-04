using EventosPeruIA.CORE.Core.DTOs;
using EventosPeruIA.CORE.Core.Entities;

namespace EventosPeruIA.CORE.Core.Interfaces;

public interface IModeloIA
{
    string ModeloEmbeddings { get; }
    Task<float[]> Embed(string texto, CancellationToken ct);
    Task<string> Responder(IReadOnlyList<TurnoChat> mensajes, CancellationToken ct);
}

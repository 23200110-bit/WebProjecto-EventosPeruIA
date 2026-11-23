namespace EventosPeruIA.CORE.Core.Interfaces;

public interface IEmbeddingService
{
    Task<int> ProcessPendingAsync(CancellationToken ct);
}

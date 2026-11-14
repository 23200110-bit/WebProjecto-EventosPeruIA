namespace EventosPeruIA.CORE.Core.Interfaces;
// Disposing without committing rolls back the repository's pending work.
public interface IRepositoryTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct);
}

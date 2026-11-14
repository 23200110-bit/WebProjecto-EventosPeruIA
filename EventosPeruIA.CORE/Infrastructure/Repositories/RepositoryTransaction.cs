using EventosPeruIA.CORE.Core.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;
namespace EventosPeruIA.CORE.Infrastructure.Repositories;
internal sealed class RepositoryTransaction(IDbContextTransaction transaction) : IRepositoryTransaction
{
    public Task CommitAsync(CancellationToken ct) => transaction.CommitAsync(ct);
    public ValueTask DisposeAsync() => transaction.DisposeAsync();
}

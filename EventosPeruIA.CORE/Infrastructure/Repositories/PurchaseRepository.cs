using System.Data;
using EventosPeruIA.CORE.Core.Entities;
using EventosPeruIA.CORE.Core.Interfaces;
using EventosPeruIA.CORE.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
namespace EventosPeruIA.CORE.Infrastructure.Repositories;
public sealed class PurchaseRepository(PostgresContext db) : IPurchaseRepository
{
    public async Task<IRepositoryTransaction> BeginTransaction(CancellationToken ct) =>
        new RepositoryTransaction(await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct));

    // Serialize retries before locking the event, even if a retry changes event ID.
    public async Task LockRequest(string key, CancellationToken ct) =>
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))", ct);

    public Task<Usuarios?> GetBuyer(int buyerId, CancellationToken ct) =>
        db.Usuarios.AsNoTracking().SingleOrDefaultAsync(u => u.UsuarioId == buyerId, ct);

    public Task<Compras?> FindByRequest(string key, CancellationToken ct) =>
        db.Compras.Include(c => c.Entradas).SingleOrDefaultAsync(c => c.CodigoTransaccion == key, ct);

    public Task<Eventos?> LockEvent(int eventId, CancellationToken ct) =>
        db.Eventos.FromSqlInterpolated($"SELECT * FROM eventos WHERE evento_id={eventId} FOR UPDATE")
            .SingleOrDefaultAsync(ct);

    public Task<int> CountSoldTickets(int eventId, CancellationToken ct) =>
        db.Entradas.CountAsync(t => t.EventoId == eventId && (t.Estado == "Valida" || t.Estado == "Utilizada"), ct);

    public async Task AddPurchase(Compras purchase, CancellationToken ct)
    {
        db.Compras.Add(purchase);
        await db.SaveChangesAsync(ct);
    }
}

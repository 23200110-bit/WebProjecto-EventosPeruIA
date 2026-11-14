using EventosPeruIA.CORE.Core.Entities;
namespace EventosPeruIA.CORE.Core.Interfaces;
public interface IPurchaseRepository
{
    Task<IRepositoryTransaction> BeginTransaction(CancellationToken ct);
    Task LockRequest(string key, CancellationToken ct);
    Task<Usuarios?> GetBuyer(int buyerId, CancellationToken ct);
    Task<Compras?> FindByRequest(string key, CancellationToken ct);
    Task<Eventos?> LockEvent(int eventId, CancellationToken ct);
    Task<int> CountSoldTickets(int eventId, CancellationToken ct);
    Task AddPurchase(Compras purchase, CancellationToken ct);
}

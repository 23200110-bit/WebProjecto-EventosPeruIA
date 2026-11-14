using EventosPeruIA.CORE.Core.DTOs;
namespace EventosPeruIA.CORE.Core.Interfaces;
public interface IPurchaseService
{
    Task<PurchaseResponse> Buy(int buyerId, PurchaseRequest request, CancellationToken ct);
}

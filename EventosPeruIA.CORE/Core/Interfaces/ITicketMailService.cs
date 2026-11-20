using EventosPeruIA.CORE.Core.DTOs;
namespace EventosPeruIA.CORE.Core.Interfaces;

public interface ITicketMailService
{
    /// <summary>Envía al comprador el resumen de la compra con el QR de cada entrada.</summary>
    Task SendPurchaseAsync(int userId, PurchaseResponse purchase, CancellationToken ct);
}

using EventosPeruIA.CORE.Core.DTOs;

namespace EventosPeruIA.CORE.Core.Interfaces
{
    public interface ITicketService
    {
        Task<List<TicketDto>> GetUserTicketsAsync(Guid userId);
        Task<VoucherDetailDto> GetVoucherByTicketIdAsync(Guid ticketId, Guid userId);
    }
}
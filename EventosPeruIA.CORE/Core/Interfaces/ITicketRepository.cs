using EventosPeruIA.CORE.Core.DTOs;

namespace EventosPeruIA.CORE.Core.Interfaces
{
    public interface ITicketRepository
    {
        Task<TicketDto?> GetTicketByIdAsync(Guid ticketId);
        Task<List<TicketDto>> GetTicketsByUserIdAsync(Guid userId);
        Task<bool> UserOwnsTicketAsync(Guid ticketId, Guid userId);
    }
}
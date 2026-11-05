using EventosPeruIA.CORE.Core.DTOs;
using EventosPeruIA.CORE.Core.Interfaces;

namespace EventosPeruIA.CORE.Infrastructure.Repositories
{
    public class TicketRepository : ITicketRepository
    {
        public async Task<List<TicketDto>> GetTicketsByUserIdAsync(Guid userId)
        {
            await Task.Delay(10);
            return new List<TicketDto>();
        }

        public async Task<TicketDto?> GetTicketByIdAsync(Guid ticketId)
        {
            await Task.Delay(10);
            return null;
        }

        public async Task<bool> UserOwnsTicketAsync(Guid ticketId, Guid userId)
        {
            await Task.Delay(10);
            return true;
        }
    }
}

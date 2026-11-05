using System.ComponentModel.DataAnnotations;
using EventosPeruIA.CORE.Core.DTOs;
using EventosPeruIA.CORE.Core.Interfaces;
namespace EventosPeruIA.CORE.Core.Services
{
    public class TicketService : ITicketService
    {
        private readonly ITicketRepository _ticketRepository;

        public TicketService(ITicketRepository ticketRepository)
        {
            _ticketRepository = ticketRepository;
        }

        public async Task<List<TicketDto>> GetUserTicketsAsync(Guid userId)
        {
            var tickets = await _ticketRepository.GetTicketsByUserIdAsync(userId);
            foreach (var ticket in tickets)
            {
                ticket.IsQrDisabled = ticket.Status != "Válida";
                ticket.IsPendingRefund = ticket.Status == "Anulada";
            }
            return tickets;
        }

        public async Task<VoucherDetailDto> GetVoucherByTicketIdAsync(Guid ticketId, Guid userId)
        {
            bool ownsTicket = await _ticketRepository.UserOwnsTicketAsync(ticketId, userId);
            if (!ownsTicket)
            {
                throw new UnauthorizedAccessException("Acceso denegado: No tienes permisos para consultar esta entrada.");
            }

            var ticket = await _ticketRepository.GetTicketByIdAsync(ticketId);
            if (ticket == null)
            {
                throw new ValidationException("La entrada especificada no existe.");
            }

            bool isActive = ticket.Status == "Válida";

            return new VoucherDetailDto
            {
                TicketId = ticket.TicketId,
                EventName = ticket.EventName,
                DateAndTitle = ticket.EventDate,
                Location = ticket.Location,
                PrimaryHolderName = ticket.CustomerFullName,
                AmountPaid = ticket.Price,
                TicketStatus = ticket.Status,
                ShortReferenceCode = ticket.ShortReferenceCode,
                QrPayload = isActive ? ticket.QrCodeData : string.Empty,
                IsActive = isActive,
                RefundStatusMessage = ticket.Status == "Anulada" ? $"Anulada. Motivo: {ticket.CancellationReason ?? "Cancelación de evento"}. Pendiente de reembolso." : string.Empty
            };
        }
    }
}

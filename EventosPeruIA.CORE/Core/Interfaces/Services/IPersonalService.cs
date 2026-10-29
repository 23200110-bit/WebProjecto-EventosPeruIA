using EventosPeruIA.CORE.Core.DTOs.Personal;
namespace EventosPeruIA.CORE.Core.Interfaces.Services;
public interface IPersonalService
{
 Task<List<TicketResponse>> GetTickets(int userId);
 Task<List<NotificationResponse>> GetNotifications(int userId);
 Task<bool> MarkRead(int userId,long id);
}

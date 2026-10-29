using EventosPeruIA.CORE.Core.DTOs.Personal;
using EventosPeruIA.CORE.Core.Interfaces.Repositories;
using EventosPeruIA.CORE.Core.Interfaces.Services;
namespace EventosPeruIA.CORE.Core.Services;
public class PersonalService : IPersonalService
{
 private readonly IPersonalRepository _repository;
 public PersonalService(IPersonalRepository repository){_repository=repository;}
 public Task<List<TicketResponse>> GetTickets(int userId){return _repository.GetTickets(userId);}
 public Task<List<NotificationResponse>> GetNotifications(int userId){return _repository.GetNotifications(userId);}
 public Task<bool> MarkRead(int userId,long id){return _repository.MarkRead(userId,id);}
}

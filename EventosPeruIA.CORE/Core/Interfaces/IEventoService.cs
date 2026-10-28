using EventosPeruIA.CORE.Core.DTOs;
namespace EventosPeruIA.CORE.Core.Interfaces
{
    public interface IEventoService
    {
        Task<bool> CancelEvento(int id, int userId, EventoCancelDTO dto);
        Task<int> CreateEvento(int userId, EventoCreateDTO dto);
        Task<EventoDTO?> GetEventoById(int id);
        Task<bool> UpdateEvento(EventoUpdateDTO dto);
        Task<bool> PatchEvento(int id, int userId, EventoPatchDTO dto);
    }
}
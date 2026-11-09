using EventosPeruIA.CORE.Core.DTOs.Eventos;

namespace EventosPeruIA.CORE.Core.Interfaces
{
    public interface IEventoService
    {
        Task<List<EventoDto>> ListarEventosAsync(EventoFiltroQueryDto filtro);
        Task<EventoDetalleDto?> ObtenerDetalleEventoAsync(int id);
        Task<int> CreateEventoAsync(int userId, EventoCreateDto dto);
        Task<bool> UpdateEventoAsync(int userId, EventoUpdateDto dto);
        Task<bool> CancelEventoAsync(int id, int userId, EventoCancelDto dto);
        Task<bool> PatchEventoAsync(int id, int userId, EventoPatchDto dto);
    }
}
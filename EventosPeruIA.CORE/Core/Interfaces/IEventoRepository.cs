using EventosPeruIA.CORE.Core.Entities;

namespace EventosPeruIA.CORE.Core.Interfaces
{
    public interface IEventoRepository
    {
        Task<bool> CancelEvento(int id, int userId, int motivoCancelacionId, string descripcionAdicional);
        Task<bool> CategoriaExists(int categoriaId);
        Task<int> CountEntradasActivas(int eventoId);
        Task<int> CreateEvento(Eventos evento);
        Task<Eventos?> GetEventoById(int id);
        Task<bool> MotivoCancelacionExists(int motivoCancelacionId);
        Task<bool> UpdateEvento(Eventos evento);
    }
}
using EventosPeruIA.CORE.Core.Entities;

namespace EventosPeruIA.CORE.Core.Interfaces
{
    public interface IEventoRepository
    {
        Task<List<Eventos>> ListarEventosPublicadosAsync(int? categoriaId, string? ubicacion, DateTime? fecha);
        Task<Eventos?> GetEventoByIdAsync(int id);
        Task<int> CreateEventoAsync(Eventos evento);
        Task<bool> UpdateEventoAsync(Eventos evento);
        Task<bool> CategoriaExistsAsync(int categoriaId);
        Task<bool> MotivoCancelacionExistsAsync(int motivoCancelacionId);
        Task<int> CountEntradasActivasAsync(int eventoId);
        Task InactivarEmbeddingsVigentesAsync(int eventoId);
        Task CrearNotificacionesCompradoresAsync(int eventoId, string tipo, string mensaje);
        Task CancelarEntradasYMarcarReembolsoAsync(int eventoId);
    }
}
using EventosPeruIA.CORE.Core.Entities;

namespace EventosPeruIA.CORE.Core.Interfaces
{
    public interface IEntradaRepository
    {
        Task<Entradas?> GetEntradaByIdAsync(Guid entradaId);
        Task<List<Entradas>> GetEntradasByUserIdAsync(int userId);
    }
}
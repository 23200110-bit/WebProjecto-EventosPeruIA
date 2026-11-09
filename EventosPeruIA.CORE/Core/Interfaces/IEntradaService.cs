using EventosPeruIA.CORE.Core.DTOs.Entradas;

namespace EventosPeruIA.CORE.Core.Interfaces
{
    public interface IEntradaService
    {
        Task<List<EntradaDto>> GetMisEntradasAsync(int userId);
        Task<VoucherDetalleDto> GetVoucherByEntradaIdAsync(Guid entradaId, int userId);
    }
}
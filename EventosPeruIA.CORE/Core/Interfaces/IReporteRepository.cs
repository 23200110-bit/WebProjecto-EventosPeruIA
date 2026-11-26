using EventosPeruIA.CORE.Core.DTOs.Reportes;

namespace EventosPeruIA.CORE.Core.Interfaces;

public interface IReporteRepository
{
    Task<DatosReporteRaw> ObtenerDatosAsync(int organizadorId, ReporteFiltroRequestDto filtro);
}
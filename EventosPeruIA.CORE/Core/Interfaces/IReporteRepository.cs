using EventosPeruIA.CORE.Core.DTOs.Reportes;

namespace EventosPeruIA.CORE.Core.Interfaces
{
    public interface IReporteRepository
    {
        Task<bool> EsOrganizadorDelEventoAsync(int organizadorId, int eventoId);
        Task<List<DatosVentaEventoRaw>> GetVentasEventoRawAsync(int organizadorId, int? eventoId);
        Task<List<DatosAsistenciaRaw>> GetAsistentesRawAsync(int organizadorId, int? eventoId, DateTime? fechaInicio, DateTime? fechaFin);
        Task<List<DatosVentaDiariaRaw>> GetVentasDiariasRawAsync(int organizadorId, int? eventoId, DateTime? fechaInicio, DateTime? fechaFin);
        Task<List<DatosReembolsoRaw>> GetReembolsosRawAsync(int organizadorId, int? eventoId, DateTime? fechaInicio, DateTime? fechaFin);
    }
}
using EventosPeruIA.CORE.Core.DTOs.Reportes;

namespace EventosPeruIA.CORE.Core.Interfaces
{
    public interface IReporteService
    {
        Task<ReporteDashboardOrganizadorDto> GetReporteDashboardOrganizadorAsync(int organizadorId, ReporteFiltroRequestDto filtro);
        Task<ArchivoExportadoResponseDto> ExportarReporteExcelAsync(int organizadorId, ReporteFiltroRequestDto filtro);
        Task<ArchivoExportadoResponseDto> ExportarReportePdfAsync(int organizadorId, ReporteFiltroRequestDto filtro);
    }
}
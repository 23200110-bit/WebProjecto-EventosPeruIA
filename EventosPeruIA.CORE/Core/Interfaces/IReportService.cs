using EventosPeruIA.CORE.Core.DTOs.Reports;

namespace EventosPeruIA.CORE.Core.Interfaces
{
    public interface IReportService
    {
        Task<ExportFileResponseDto> ExportReportToExcelAsync(Guid organizerId, ReportFilterRequestDto filter);
        Task<ExportFileResponseDto> ExportReportToPdfAsync(Guid organizerId, ReportFilterRequestDto filter);
        Task<OrganizerDashboardReportDto> GetOrganizerDashboardReportAsync(Guid organizerId, ReportFilterRequestDto filter);
    }
}
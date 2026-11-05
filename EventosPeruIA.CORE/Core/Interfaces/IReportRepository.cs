using EventosPeruIA.CORE.Core.DTOs.Reports;

namespace EventosPeruIA.CORE.Core.Interfaces
{
    public interface IReportRepository
    {
        Task<List<DailySalesGraphDto>> GetOrganizerDailySalesAsync(Guid organizerId, ReportFilterRequestDto filter);
        Task<List<EventSalesSummaryDto>> GetOrganizerEventSummariesAsync(Guid organizerId, ReportFilterRequestDto filter);
        Task<bool> IsOrganizerOfEventAsync(Guid organizerId, Guid eventId);
    }
}
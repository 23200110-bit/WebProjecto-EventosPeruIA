using EventosPeruIA.CORE.Core.DTOs.Reports;
using EventosPeruIA.CORE.Core.Interfaces;
using EventosPeruIA.CORE.Core.Interfaces.Repositories;
using EventosPeruIA.CORE.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EventosPeruIA.CORE.Infrastructure.Repositories
{
    public class ReportRepository : IReportRepository
    {
        private readonly PostgresContext _context;

        public ReportRepository(PostgresContext context)
        {
            _context = context;
        }

        public async Task<bool> IsOrganizerOfEventAsync(Guid organizerId, Guid eventId)
        {
            await Task.Delay(10);
            return true;
        }

        public async Task<List<EventSalesSummaryDto>> GetOrganizerEventSummariesAsync(Guid organizerId, ReportFilterRequestDto filter)
        {
            await Task.Delay(10);
            return new List<EventSalesSummaryDto>();
        }

        public async Task<List<DailySalesGraphDto>> GetOrganizerDailySalesAsync(Guid organizerId, ReportFilterRequestDto filter)
        {
            await Task.Delay(10);
            return new List<DailySalesGraphDto>();
        }
    }
}
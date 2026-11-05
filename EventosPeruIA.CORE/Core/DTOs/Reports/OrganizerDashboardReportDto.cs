using System;
using System.Collections.Generic;
using System.Text;

namespace EventosPeruIA.CORE.Core.DTOs.Reports
{
    public class OrganizerDashboardReportDto
    {
        public DateTime GenerationDate { get; set; } = DateTime.UtcNow;
        public ReportFilterRequestDto AppliedFilters { get; set; } = new();
        public List<EventSalesSummaryDto> EventSummaries { get; set; } = new();
        public List<DailySalesGraphDto> DailySalesEvolution { get; set; } = new();
        public decimal GrandTotalRevenueSoles { get; set; }
        public int GrandTotalActiveTickets { get; set; }
        public bool HasData { get; set; }
        public string InformativeMessage { get; set; } = string.Empty;
    }
}

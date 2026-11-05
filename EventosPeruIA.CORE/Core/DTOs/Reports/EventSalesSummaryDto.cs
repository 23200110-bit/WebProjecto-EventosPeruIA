using System;
using System.Collections.Generic;
using System.Text;

namespace EventosPeruIA.CORE.Core.DTOs.Reports
{
    public class EventSalesSummaryDto
    {
        public Guid EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public int ValidTicketsSold { get; set; }
        public int UsedTickets { get; set; }
        public int TotalActiveTickets => ValidTicketsSold + UsedTickets;
        public int AvailableCapacity { get; set; }
        public decimal TotalRevenueSoles { get; set; }
        public int CancelledTicketsCount { get; set; }
        public decimal PendingRefundAmountSoles { get; set; }
    }
}

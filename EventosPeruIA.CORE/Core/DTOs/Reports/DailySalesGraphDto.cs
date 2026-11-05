using System;
using System.Collections.Generic;
using System.Text;

namespace EventosPeruIA.CORE.Core.DTOs.Reports
{
    public class DailySalesGraphDto
    {
        public DateTime Date { get; set; }
        public int TicketsSold { get; set; }
        public decimal TotalAmountSoles { get; set; }
    }
}

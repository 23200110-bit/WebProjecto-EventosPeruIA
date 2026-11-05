using System;
using System.Collections.Generic;
using System.Text;

namespace EventosPeruIA.CORE.Core.DTOs.Reports
{
    public class ReportFilterRequestDto
    {
        public Guid? EventId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}

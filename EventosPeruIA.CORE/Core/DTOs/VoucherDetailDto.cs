using System;
using System.Collections.Generic;
using System.Text;

namespace EventosPeruIA.CORE.Core.DTOs
{
    public class VoucherDetailDto
    {
        public Guid TicketId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime DateAndTitle { get; set; }
        public string Location { get; set; } = string.Empty;
        public string PrimaryHolderName { get; set; } = string.Empty;
        public decimal AmountPaid { get; set; }
        public string TicketStatus { get; set; } = string.Empty;
        public string ShortReferenceCode { get; set; } = string.Empty;
        public string QrPayload { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string RefundStatusMessage { get; set; } = string.Empty;
    }
}

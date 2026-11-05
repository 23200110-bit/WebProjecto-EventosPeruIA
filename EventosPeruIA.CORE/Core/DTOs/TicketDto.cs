namespace EventosPeruIA.CORE.Core.DTOs;

public class TicketDto
{
    public Guid TicketId { get; set; }
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string CustomerFullName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ShortReferenceCode { get; set; } = string.Empty;
    public string QrCodeData { get; set; } = string.Empty;
    public bool IsQrDisabled { get; set; }
    public string? CancellationReason { get; set; }
    public bool IsPendingRefund { get; set; }
}
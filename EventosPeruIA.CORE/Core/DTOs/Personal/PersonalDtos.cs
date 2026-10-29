namespace EventosPeruIA.CORE.Core.DTOs.Personal;
public class TicketResponse
{
 public Guid TicketId {get;set;} public int EventId {get;set;} public string? Event {get;set;}
 public string? Holder {get;set;} public string? ShortCode {get;set;} public string? Status {get;set;}
 public string? EventStatus {get;set;} public string? RefundStatus {get;set;} public string? CancellationReason {get;set;}
 public string? Venue {get;set;} public string? Address {get;set;} public string? City {get;set;}
 public DateTime? StartDate {get;set;} public decimal Price {get;set;} public string Currency {get;set;}="PEN";
}
public class NotificationResponse
{
 public long Id {get;set;} public string? Title {get;set;} public string Message {get;set;}="";
 public bool Read {get;set;} public DateTime CreatedAt {get;set;}
}

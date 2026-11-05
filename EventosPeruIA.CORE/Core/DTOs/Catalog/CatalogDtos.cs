using System.ComponentModel.DataAnnotations;
namespace EventosPeruIA.CORE.Core.DTOs.Catalog;
public sealed class EventSearchRequest
{
    [StringLength(150)] public string? Query {get;set;}
    [Range(1,int.MaxValue)] public int? CategoryId {get;set;}
    [StringLength(100)] public string? City {get;set;}
    public DateTimeOffset? From {get;set;}
    public DateTimeOffset? To {get;set;}
    [Range(1,10000)] public int Page {get;set;}=1;
    [Range(1,50)] public int PageSize {get;set;}=20;
}
public class CategoryResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}
public class EventCatalogItem
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int CategoryId { get; set; }
    public string Category { get; set; } = "";
    public string City { get; set; } = "";
    public string Address { get; set; } = "";
    public string? Venue { get; set; }
    public string Organizer { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "";
    public int Capacity { get; set; }
    public int AvailableSpots { get; set; }
    public string Status { get; set; } = "";
    public string? ImageUrl { get; set; }
}


//PARTE DE SANTIAGO
public class EventDetailResponse
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string? Description { get; set; }
	public string? ImageUrl { get; set; }
	public int CategoryId { get; set; }
	public string Category { get; set; } = string.Empty;
	public string? Address { get; set; }
	public string? City { get; set; }
	public string? Venue { get; set; }
	public string Organizer { get; set; } = string.Empty;
	public DateTime StartDate { get; set; }
	public DateTime? EndDate { get; set; }
	public decimal Price { get; set; }
	public string Currency { get; set; } = "PEN";
	public string Status { get; set; } = string.Empty;

	// Cálculo de aforo reactivo (US-006)
	public int Capacity { get; set; }
	public int SoldTickets { get; set; }
	public int AvailableSpots { get; set; }
	public bool IsSoldOut { get; set; }
}

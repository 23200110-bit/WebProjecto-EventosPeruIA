namespace EventosPeruIA.CORE.Core.DTOs.Recommendations;

public sealed class RecommendationResponse
{
    public int EventId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ImageUrl { get; init; }
    public int? CategoryId { get; init; }
    public string? Category { get; init; }
    public string? VenueName { get; init; }
    public string? Address { get; init; }
    public string? City { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public decimal? Price { get; init; }
    public string? Currency { get; init; }
    public int? AvailableSpots { get; init; }
    public decimal Score { get; init; }
    public string Reason { get; init; } = string.Empty;
}

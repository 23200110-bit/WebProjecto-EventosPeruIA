namespace EventosPeruIA.CORE.Core.DTOs.EventReviews;

public sealed class CreateEventReviewRequest
{
    public Guid TicketId { get; init; }
    public short Rating { get; init; }
    public string? Comment { get; init; }
}

public sealed class EventReviewResponse
{
    public int ReviewId { get; init; }
    public int EventId { get; init; }
    public int UserId { get; init; }
    public short Rating { get; init; }
    public string? Comment { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public sealed class EventReviewSummaryResponse
{
    public int EventId { get; init; }
    public decimal AverageRating { get; init; }
    public int TotalReviews { get; init; }
    public IReadOnlyCollection<EventReviewResponse> Reviews { get; init; } = [];
}

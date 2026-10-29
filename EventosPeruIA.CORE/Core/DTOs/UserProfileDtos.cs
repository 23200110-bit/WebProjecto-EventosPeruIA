namespace EventosPeruIA.CORE.Core.DTOs;

public sealed class UserProfileResponse
{
    public int UserId { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? PhotoUrl { get; init; }
    public string? City { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public IReadOnlyCollection<InterestResponse> Interests { get; init; } = [];
}

public sealed class UpdateUserProfileRequest
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? PhotoUrl { get; init; }
    public string? City { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public IReadOnlyCollection<int> InterestCategoryIds { get; init; } = [];
}

public sealed class InterestResponse
{
    public int CategoryId { get; init; }
    public string Name { get; init; } = string.Empty;
}

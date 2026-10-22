using EventosPeruIA.CORE.Core.DTOs.UserProfile;

namespace EventosPeruIA.CORE.Core.Interfaces.Services;

public interface IUserProfileService
{
    Task<UserProfileResponse?> GetAsync(int userId, CancellationToken cancellationToken = default);
    Task<UserProfileResponse> UpdateAsync(int userId, UpdateUserProfileRequest request, CancellationToken cancellationToken = default);
}

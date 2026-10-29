using EventosPeruIA.CORE.Core.DTOs.Auth;
namespace EventosPeruIA.CORE.Core.Interfaces.Services;
public interface IAccountService
{
    Task<AccountResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AccountResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default);
}

using EventosPeruIA.CORE.Core.DTOs.Auth;
namespace EventosPeruIA.CORE.Core.Interfaces.Services;

public interface IGoogleAuthService
{
    /// <summary>Inicia sesión (o crea la cuenta) con un ID token de Google. Null si el token o la cuenta no son válidos.</summary>
    Task<AccountResponse?> LoginAsync(GoogleLoginRequest request, CancellationToken ct = default);
}

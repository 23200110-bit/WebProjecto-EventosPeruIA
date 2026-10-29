using EventosPeruIA.CORE.Core.DTOs.Auth;
namespace EventosPeruIA.CORE.Core.Interfaces.Services
{
    public interface IJWTService
    {
        string GenerateJWToken(AccountResponse user);
    }
}

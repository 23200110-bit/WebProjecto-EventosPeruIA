using EventosPeruIA.CORE.Core.DTOs;
namespace EventosPeruIA.CORE.Core.Interfaces;
public interface IPasswordResetService
{
    Task Request(ForgotPasswordRequest request, CancellationToken ct);
    Task Reset(ResetPasswordRequest request, CancellationToken ct);
}

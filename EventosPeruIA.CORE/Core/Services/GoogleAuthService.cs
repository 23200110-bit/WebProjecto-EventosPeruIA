using System.ComponentModel.DataAnnotations;
using EventosPeruIA.CORE.Core.DTOs.Auth;
using EventosPeruIA.CORE.Core.Entities;
using EventosPeruIA.CORE.Core.Interfaces;
using EventosPeruIA.CORE.Core.Interfaces.Repositories;
using EventosPeruIA.CORE.Core.Interfaces.Services;
namespace EventosPeruIA.CORE.Core.Services;

public sealed class GoogleAuthService(IAccountRepository repository, IGoogleTokenValidator validator) : IGoogleAuthService
{
    public async Task<AccountResponse?> LoginAsync(GoogleLoginRequest request, CancellationToken ct = default)
    {
        Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
        var identity = await validator.ValidateAsync(request.IdToken, ct);
        if (identity is null) return null;

        var user = await repository.FindByEmailAsync(identity.Email, ct);
        if (user is null)
        {
            var nombres = string.IsNullOrWhiteSpace(identity.GivenName) ? identity.Email.Split('@')[0] : identity.GivenName;
            var apellidos = string.IsNullOrWhiteSpace(identity.FamilyName) ? "-" : identity.FamilyName;
            user = new Usuarios
            {
                Nombres = Cut(nombres, 100),
                Apellidos = Cut(apellidos, 100),
                Correo = identity.Email,
                Estado = "Activo",
                ProveedorAuth = "Google",
                IdExterno = identity.Subject,
                EsAdministrador = false,
                CorreoVerificado = true,
                FechaVerificacion = DateTime.UtcNow,
                FechaRegistro = DateTime.UtcNow
            };
            await repository.AddAsync(user, ct);
        }
        else if (user.Estado != "Activo" || user.BloqueadoHasta > DateTime.UtcNow)
        {
            return null;
        }

        return new AccountResponse
        {
            UserId = user.UsuarioId,
            FirstName = user.Nombres,
            LastName = user.Apellidos,
            Email = user.Correo,
            IsAdministrator = user.EsAdministrador,
            CredentialVersion = PasswordResetService.Hash(user.ContrasenaHash ?? "")
        };
    }

    private static string Cut(string value, int max) => value.Trim().Length <= max ? value.Trim() : value.Trim()[..max];
}

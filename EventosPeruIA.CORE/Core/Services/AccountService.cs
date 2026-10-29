using System.ComponentModel.DataAnnotations;
using EventosPeruIA.CORE.Core.DTOs.Auth;
using EventosPeruIA.CORE.Core.Entities;
using EventosPeruIA.CORE.Core.Interfaces.Repositories;
using EventosPeruIA.CORE.Core.Interfaces.Services;
using Microsoft.AspNetCore.Identity;
namespace EventosPeruIA.CORE.Core.Services;
public class AccountService : IAccountService
{
    private readonly IAccountRepository repository;
    private readonly PasswordHasher<Usuarios> hasher = new PasswordHasher<Usuarios>();
    public AccountService(IAccountRepository accountRepository)
    {
        repository = accountRepository;
    }
    private static readonly Usuarios DummyUser = new();
    private static readonly string DummyHash = new PasswordHasher<Usuarios>().HashPassword(DummyUser, Guid.NewGuid().ToString());
    public async Task<AccountResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            throw new ValidationException("Nombres y apellidos son obligatorios.");
        var email = request.Email.Trim().ToLowerInvariant();
        if (await repository.FindByEmailAsync(email, ct) is not null)
            throw new InvalidOperationException("No se pudo registrar la cuenta con esos datos.");
        var user = new Usuarios { Nombres=request.FirstName.Trim(), Apellidos=request.LastName.Trim(),
            Correo=email, Estado="Activo", ProveedorAuth="Local", EsAdministrador=false,
            CorreoVerificado=false, FechaRegistro=DateTime.UtcNow };
        user.ContrasenaHash = hasher.HashPassword(user, request.Password);
        await repository.AddAsync(user, ct);
        return Map(user);
    }
    public async Task<AccountResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
        var user = await repository.FindByEmailAsync(request.Email.Trim().ToLowerInvariant(), ct);
        var hash = user?.ContrasenaHash ?? DummyHash;
        PasswordVerificationResult verified;
        try { verified = hasher.VerifyHashedPassword(user ?? DummyUser, hash, request.Password); }
        catch (FormatException) { return null; }
        if (user is null || user.ContrasenaHash is null || verified == PasswordVerificationResult.Failed ||
            user.Estado != "Activo" || user.BloqueadoHasta > DateTime.UtcNow) return null;
        return Map(user);
    }
    private static AccountResponse Map(Usuarios u) => new AccountResponse { UserId = u.UsuarioId, FirstName = u.Nombres, LastName = u.Apellidos, Email = u.Correo, IsAdministrator = u.EsAdministrador };
}

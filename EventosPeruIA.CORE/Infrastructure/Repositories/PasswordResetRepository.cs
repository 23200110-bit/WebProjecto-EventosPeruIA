using System.Data;
using EventosPeruIA.CORE.Core.Entities;
using EventosPeruIA.CORE.Core.Interfaces;
using EventosPeruIA.CORE.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
namespace EventosPeruIA.CORE.Infrastructure.Repositories;
public sealed class PasswordResetRepository(PostgresContext db) : IPasswordResetRepository
{
    public async Task<IRepositoryTransaction> BeginTransaction(CancellationToken ct) =>
        new RepositoryTransaction(await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct));

    public Task<Usuarios?> FindUserByEmail(string email, CancellationToken ct) =>
        db.Usuarios.AsNoTracking().SingleOrDefaultAsync(u => u.Correo == email, ct);

    public Task<Usuarios?> LockUser(int userId, CancellationToken ct) =>
        db.Usuarios.FromSqlInterpolated($"SELECT * FROM usuarios WHERE usuario_id={userId} FOR UPDATE")
            .SingleOrDefaultAsync(ct);

    public Task<bool> HasRecentRequest(int userId, DateTime since, CancellationToken ct) =>
        db.TokensRecuperacionContrasena.AnyAsync(t => t.UsuarioId == userId && t.FechaCreacion > since, ct);

    public async Task AddToken(TokensRecuperacionContrasena token, CancellationToken ct)
    {
        db.TokensRecuperacionContrasena.Add(token);
        await db.SaveChangesAsync(ct);
    }

    public Task<int?> FindTokenUser(string hash, CancellationToken ct) =>
        db.TokensRecuperacionContrasena.AsNoTracking().Where(t => t.TokenHash == hash)
            .Select(t => (int?)t.UsuarioId).SingleOrDefaultAsync(ct);

    public Task<TokensRecuperacionContrasena?> LockToken(string hash, CancellationToken ct) =>
        db.TokensRecuperacionContrasena.FromSqlInterpolated($"SELECT * FROM tokens_recuperacion_contrasena WHERE token_hash={hash} FOR UPDATE")
            .SingleOrDefaultAsync(ct);

    public async Task MarkAllTokensUsed(int userId, DateTime now, CancellationToken ct) =>
        await db.TokensRecuperacionContrasena.Where(t => t.UsuarioId == userId && !t.Utilizado)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.Utilizado, true).SetProperty(t => t.FechaUso, now), ct);

    public async Task SaveChanges(CancellationToken ct) => await db.SaveChangesAsync(ct);
}

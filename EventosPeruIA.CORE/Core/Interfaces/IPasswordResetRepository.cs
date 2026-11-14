using EventosPeruIA.CORE.Core.Entities;
namespace EventosPeruIA.CORE.Core.Interfaces;
public interface IPasswordResetRepository
{
    Task<IRepositoryTransaction> BeginTransaction(CancellationToken ct);
    Task<Usuarios?> FindUserByEmail(string email, CancellationToken ct);
    Task<Usuarios?> LockUser(int userId, CancellationToken ct);
    Task<bool> HasRecentRequest(int userId, DateTime since, CancellationToken ct);
    Task AddToken(TokensRecuperacionContrasena token, CancellationToken ct);
    Task<int?> FindTokenUser(string hash, CancellationToken ct);
    Task<TokensRecuperacionContrasena?> LockToken(string hash, CancellationToken ct);
    Task MarkAllTokensUsed(int userId, DateTime now, CancellationToken ct);
    Task SaveChanges(CancellationToken ct);
}

using EventosPeruIA.CORE.Core.Entities;
namespace EventosPeruIA.CORE.Core.Interfaces.Repositories;
public interface IAccountRepository
{
    Task<Usuarios?> FindByEmailAsync(string email, CancellationToken ct = default);
    Task<Usuarios?> FindByIdAsync(int id, CancellationToken ct = default);
    Task AddAsync(Usuarios user, CancellationToken ct = default);
}

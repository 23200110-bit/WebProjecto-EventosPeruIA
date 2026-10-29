using EventosPeruIA.CORE.Core.Entities;

namespace EventosPeruIA.CORE.Core.Interfaces;

public interface IUserProfileRepository
{
    Task<Usuarios?> GetByIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Categorias>> GetCategoriesAsync(IEnumerable<int> categoryIds, CancellationToken cancellationToken = default);
    Task UpdateAsync(Usuarios user, IEnumerable<UsuarioIntereses> interests, CancellationToken cancellationToken = default);
}

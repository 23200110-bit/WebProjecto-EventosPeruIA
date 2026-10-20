using EventosPeruIA.CORE.Core.Entities;
using EventosPeruIA.CORE.Core.Interfaces.Repositories;
using EventosPeruIA.CORE.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EventosPeruIA.CORE.Infrastructure.Repositories;

public sealed class UserProfileRepository(PostgresContext context) : IUserProfileRepository
{
    public Task<Usuarios?> GetByIdAsync(int userId, CancellationToken cancellationToken = default) =>
        context.Usuarios
            .Include(user => user.UsuarioIntereses)
            .ThenInclude(interest => interest.Categoria)
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.UsuarioId == userId, cancellationToken);

    public async Task<IReadOnlyCollection<Categorias>> GetCategoriesAsync(
        IEnumerable<int> categoryIds,
        CancellationToken cancellationToken = default)
    {
        var ids = categoryIds.Distinct().ToArray();
        return await context.Categorias
            .AsNoTracking()
            .Where(category => category.Activo && ids.Contains(category.CategoriaId))
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        Usuarios user,
        IEnumerable<UsuarioIntereses> interests,
        CancellationToken cancellationToken = default)
    {
        context.Usuarios.Update(user);

        var existingInterests = await context.UsuarioIntereses
            .Where(interest => interest.UsuarioId == user.UsuarioId)
            .ToListAsync(cancellationToken);

        context.UsuarioIntereses.RemoveRange(existingInterests);
        await context.UsuarioIntereses.AddRangeAsync(interests, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}

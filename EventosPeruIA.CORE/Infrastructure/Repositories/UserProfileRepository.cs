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
        // Reload one tracked aggregate; never attach the disconnected navigation graph.
        var tracked = await context.Usuarios.Include(u => u.UsuarioIntereses)
            .SingleAsync(u => u.UsuarioId == user.UsuarioId, cancellationToken);
        tracked.Nombres = user.Nombres;
        tracked.Apellidos = user.Apellidos;
        tracked.Telefono = user.Telefono;
        tracked.FotoUrl = user.FotoUrl;
        tracked.CiudadUbicacion = user.CiudadUbicacion;
        tracked.Latitud = user.Latitud;
        tracked.Longitud = user.Longitud;
        tracked.FechaActualizacion = user.FechaActualizacion;
        var selected = interests.ToDictionary(i => i.CategoriaId);
        foreach (var old in tracked.UsuarioIntereses.ToArray())
            if (!selected.ContainsKey(old.CategoriaId)) context.UsuarioIntereses.Remove(old);
        var existing = tracked.UsuarioIntereses.Select(i => i.CategoriaId).ToHashSet();
        foreach (var item in selected.Values)
            if (!existing.Contains(item.CategoriaId)) tracked.UsuarioIntereses.Add(item);
        await context.SaveChangesAsync(cancellationToken);
    }
}

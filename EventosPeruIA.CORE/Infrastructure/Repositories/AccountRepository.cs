using EventosPeruIA.CORE.Core.Entities;
using EventosPeruIA.CORE.Core.Interfaces.Repositories;
using EventosPeruIA.CORE.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
namespace EventosPeruIA.CORE.Infrastructure.Repositories;
public class AccountRepository : IAccountRepository
{
    private readonly PostgresContext context;
    public AccountRepository(PostgresContext context)
    {
        this.context = context;
    }

    public Task<Usuarios?> FindByEmailAsync(string email, CancellationToken ct = default) =>
        context.Usuarios.AsNoTracking().SingleOrDefaultAsync(u => u.Correo == email, ct);
    public Task<Usuarios?> FindByIdAsync(int id, CancellationToken ct = default) =>
        context.Usuarios.AsNoTracking().SingleOrDefaultAsync(u => u.UsuarioId == id, ct);
    public async Task AddAsync(Usuarios user, CancellationToken ct = default)
    {
        context.Usuarios.Add(user);
        try { await context.SaveChangesAsync(ct); }
        catch (DbUpdateException e) when (e.InnerException is Npgsql.PostgresException { SqlState: "23505" })
        { throw new InvalidOperationException("No se pudo registrar la cuenta con esos datos."); }
    }
}

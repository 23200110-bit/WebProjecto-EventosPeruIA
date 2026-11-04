using EventosPeruIA.CORE.Core.DTOs;
using EventosPeruIA.CORE.Core.Entities;
using EventosPeruIA.CORE.Core.Interfaces;
using EventosPeruIA.CORE.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EventosPeruIA.CORE.Infrastructure.Repositories;

public sealed class AdminRepository(PostgresContext db) : IAdminRepository
{
    public string EstadoRetirado => "Retirado";
    public async Task<PaginaDTO<UsuarioAdminDTO>> ListarUsuarios(string? buscar, string? estado, int pagina, int tamano, CancellationToken ct)
    {
        var query = db.Usuarios.AsNoTracking();
        if (estado != null) query = query.Where(u => u.Estado == estado);
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var text = buscar.Trim().ToLowerInvariant();
            query = query.Where(u => u.Nombres.ToLower().Contains(text) || u.Apellidos.ToLower().Contains(text) || u.Correo.ToLower().Contains(text));
        }
        var total = await query.CountAsync(ct);
        var rows = await query.OrderBy(u => u.UsuarioId).Skip((pagina - 1) * tamano).Take(tamano)
            .Select(u => new UsuarioAdminDTO(u.UsuarioId, u.Nombres, u.Apellidos, u.Correo, u.Estado, u.EsAdministrador)).ToListAsync(ct);
        return new(total, pagina, tamano, rows);
    }
    public async Task<PaginaDTO<EventoAdminDTO>> ListarEventos(string? buscar, string? estado, int pagina, int tamano, CancellationToken ct)
    {
        var query = db.Eventos.AsNoTracking();
        if (estado != null) query = query.Where(e => e.Estado == estado);
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var text = buscar.Trim().ToLowerInvariant();
            query = query.Where(e => e.Nombre.ToLower().Contains(text));
        }
        var total = await query.CountAsync(ct);
        var rows = await query.OrderBy(e => e.EventoId).Skip((pagina - 1) * tamano).Take(tamano)
            .Select(e => new EventoAdminDTO(e.EventoId, e.OrganizadorId, e.Nombre, e.Estado, e.EstadoModeracion)).ToListAsync(ct);
        return new(total, pagina, tamano, rows);
    }
    public Task<Usuarios?> Usuario(int id, CancellationToken ct) => db.Usuarios.AsNoTracking().SingleOrDefaultAsync(u => u.UsuarioId == id, ct);
    public Task<Eventos?> Evento(int id, CancellationToken ct) => db.Eventos.AsNoTracking().SingleOrDefaultAsync(e => e.EventoId == id, ct);
    public async Task Guardar(AccionesModeracion accion, CancellationToken ct)
    {
        // Las funciones existentes guardan el estado y su auditoría de forma atómica.
        if (accion.EntidadTipo == "Usuario")
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT public.fn_moderar_usuario({accion.AdministradorId}, {accion.EntidadId}, CAST({accion.TipoAccion} AS varchar), {accion.Motivo})", ct);
        else
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT public.fn_moderar_evento({accion.AdministradorId}, {accion.EntidadId}, CAST({accion.TipoAccion} AS varchar), {accion.Motivo})", ct);
    }
}

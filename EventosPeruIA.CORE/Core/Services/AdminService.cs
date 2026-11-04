using EventosPeruIA.CORE.Core.DTOs;
using EventosPeruIA.CORE.Core.Entities;
using EventosPeruIA.CORE.Core.Interfaces;

namespace EventosPeruIA.CORE.Core.Services;

public sealed class AdminService(IAdminRepository repository, TimeProvider clock) : IAdminService
{
    public async Task<PaginaDTO<UsuarioAdminDTO>> Usuarios(string? buscar, string? estado, int pagina, int tamano, CancellationToken ct)
    {
        if (estado != null && estado is not ("Activo" or "Bloqueado" or "Inactivo")) throw new ProyectoException(400, "Estado de usuario inválido.");
        return await repository.ListarUsuarios(buscar, estado, pagina, tamano, ct);
    }
    public async Task<PaginaDTO<EventoAdminDTO>> Eventos(string? buscar, string? estado, int pagina, int tamano, CancellationToken ct)
    {
        if (estado != null && estado is not ("Borrador" or "Publicado" or "Finalizado" or "Cancelado")) throw new ProyectoException(400, "Estado de evento inválido.");
        return await repository.ListarEventos(buscar, estado, pagina, tamano, ct);
    }
    public async Task EstadoUsuario(int administradorId, int id, CambiarEstadoUsuarioDTO dto, CancellationToken ct)
    {
        Confirmar(dto.Confirmado, dto.Motivo);
        if (administradorId == id) throw new ProyectoException(409, "No puedes cambiar tu propio estado desde moderación.");
        var usuario = await repository.Usuario(id, ct) ?? throw new ProyectoException(404, "Usuario no encontrado.");
        if (usuario.Estado == dto.Estado) throw new ProyectoException(409, "La cuenta ya tiene ese estado.");
        var anterior = usuario.Estado;
        usuario.Estado = dto.Estado;
        usuario.FechaActualizacion = clock.GetUtcNow().UtcDateTime;
        var auditoria = Auditoria(administradorId, dto.Estado == "Bloqueado" ? "BloquearUsuario" : "ReactivarUsuario", "Usuario", id, dto.Motivo);
        auditoria.EstadoAnterior = anterior;
        auditoria.EstadoNuevo = dto.Estado;
        await repository.Guardar(auditoria, ct);
    }
    public async Task ModerarEvento(int administradorId, int id, ModerarEventoDTO dto, CancellationToken ct)
    {
        Confirmar(dto.Confirmado, dto.Motivo);
        var evento = await repository.Evento(id, ct) ?? throw new ProyectoException(404, "Evento no encontrado.");
        if (evento.Estado is "Cancelado" or "Finalizado") throw new ProyectoException(409, "No se puede moderar un evento cancelado o finalizado.");
        var estado = dto.Accion == "Retirar" ? repository.EstadoRetirado : "Suspendido";
        if (evento.EstadoModeracion == estado) throw new ProyectoException(409, "El evento ya tiene esa moderación.");
        // Respetar los estados admitidos por cada esquema.
        var anterior = evento.EstadoModeracion;
        evento.EstadoModeracion = estado;
        evento.FechaActualizacion = clock.GetUtcNow().UtcDateTime;
        var auditoria = Auditoria(administradorId, dto.Accion == "Retirar" ? "RetirarEvento" : "SuspenderEvento", "Evento", id, dto.Motivo);
        auditoria.EstadoAnterior = anterior;
        auditoria.EstadoNuevo = estado;
        await repository.Guardar(auditoria, ct);
    }
    private static void Confirmar(bool confirmado, string motivo)
    {
        if (!confirmado || motivo.Trim().Length < 3) throw new ProyectoException(400, "Confirma la acción e indica un motivo de al menos tres caracteres.");
    }
    private AccionesModeracion Auditoria(int admin, string tipo, string entidad, int id, string motivo) => new()
    {
        AdministradorId = admin, TipoAccion = tipo, EntidadTipo = entidad, EntidadId = id,
        Motivo = motivo.Trim(), FechaHora = clock.GetUtcNow().UtcDateTime
    };
}

using System;
using System.Collections.Generic;
using System.Net;

namespace EventosPeruIA.CORE.Core.Entities;

/// <summary>
/// Auditoría inmutable de acciones de administradores (US-016). No se edita ni se borra.
/// </summary>
public partial class AccionesModeracion
{
    public long AccionId { get; set; }

    public int AdministradorId { get; set; }

    public string TipoAccion { get; set; } = null!;

    public string EntidadTipo { get; set; } = null!;

    /// <summary>
    /// Id del usuario, evento o reseña según entidad_tipo.
    /// </summary>
    public int EntidadId { get; set; }

    public string? EstadoAnterior { get; set; }

    public string? EstadoNuevo { get; set; }

    public string? Motivo { get; set; }

    public IPAddress? IpOrigen { get; set; }

    public DateTime FechaHora { get; set; }

    public virtual Usuarios Administrador { get; set; } = null!;
}

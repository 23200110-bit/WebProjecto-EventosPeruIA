using System;
using System.Collections.Generic;

namespace EventosPeruIA.CORE.Core.Entities;

/// <summary>
/// Avisos para el usuario: compra, recordatorio, cambios, cancelación y moderación (US-014).
/// </summary>
public partial class Notificaciones
{
    public long NotificacionId { get; set; }

    public int UsuarioId { get; set; }

    public int? EventoId { get; set; }

    public string Tipo { get; set; } = null!;

    public string? Titulo { get; set; }

    public string Mensaje { get; set; } = null!;

    public string? Datos { get; set; }

    public bool Leida { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaLectura { get; set; }

    public virtual Eventos? Evento { get; set; }

    public virtual Usuarios Usuario { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace EventosPeruIA.CORE.Core.Entities;

/// <summary>
/// Calificaciones de asistentes que usaron su entrada en un evento finalizado (US-013).
/// </summary>
public partial class Resenas
{
    public int ResenaId { get; set; }

    public Guid EntradaId { get; set; }

    public int EventoId { get; set; }

    public int UsuarioId { get; set; }

    public short Calificacion { get; set; }

    public string? Comentario { get; set; }

    /// <summary>
    /// Activa o Moderada (oculta). Solo cuenta la reseña activa por usuario y evento.
    /// </summary>
    public string Estado { get; set; } = null!;

    public string? MotivoModeracion { get; set; }

    public int? ModeradaPorId { get; set; }

    public DateTime? FechaModeracion { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaActualizacion { get; set; }

    public virtual Entradas Entradas { get; set; } = null!;

    public virtual Eventos Evento { get; set; } = null!;

    public virtual Usuarios? ModeradaPor { get; set; }

    public virtual Usuarios Usuario { get; set; } = null!;
}

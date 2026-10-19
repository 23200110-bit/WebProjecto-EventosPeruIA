using System;
using System.Collections.Generic;

namespace EventosPeruIA.CORE.Core.Entities;

/// <summary>
/// Catálogo de motivos válidos para cancelar un evento (US-009).
/// </summary>
public partial class MotivosCancelacion
{
    public int MotivoCancelacionId { get; set; }

    public string Codigo { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public bool Activo { get; set; }

    public virtual ICollection<EventosCancelacion> EventosCancelacion { get; set; } = new List<EventosCancelacion>();
}

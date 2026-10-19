using System;
using System.Collections.Generic;

namespace EventosPeruIA.CORE.Core.Entities;

/// <summary>
/// Vistas, búsquedas, compras y favoritos del usuario: señales para las recomendaciones (US-007).
/// </summary>
public partial class UsuarioEventoInteraccion
{
    public long InteraccionId { get; set; }

    public int UsuarioId { get; set; }

    public int? EventoId { get; set; }

    public string TipoInteraccion { get; set; } = null!;

    public string? TerminoBusqueda { get; set; }

    public DateTime FechaHora { get; set; }

    public virtual Eventos? Evento { get; set; }

    public virtual Usuarios Usuario { get; set; } = null!;
}

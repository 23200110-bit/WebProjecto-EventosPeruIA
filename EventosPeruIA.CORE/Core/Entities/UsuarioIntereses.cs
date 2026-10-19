using System;
using System.Collections.Generic;

namespace EventosPeruIA.CORE.Core.Entities;

/// <summary>
/// Categorías de interés declaradas por el usuario (US-003). Señal explícita de recomendación.
/// </summary>
public partial class UsuarioIntereses
{
    public int UsuarioId { get; set; }

    public int CategoriaId { get; set; }

    public DateTime FechaRegistro { get; set; }

    public virtual Categorias Categoria { get; set; } = null!;

    public virtual Usuarios Usuario { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace EventosPeruIA.CORE.Core.Entities;

/// <summary>
/// Catálogo de categorías de eventos (conciertos, talleres, ferias, charlas, deportes).
/// </summary>
public partial class Categorias
{
    public int CategoriaId { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public string? Icono { get; set; }

    public int Orden { get; set; }

    public bool Activo { get; set; }

    public DateTime FechaCreacion { get; set; }

    public virtual ICollection<Eventos> Eventos { get; set; } = new List<Eventos>();

    public virtual ICollection<UsuarioIntereses> UsuarioIntereses { get; set; } = new List<UsuarioIntereses>();
}

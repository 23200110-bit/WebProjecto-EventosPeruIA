using System;
using System.Collections.Generic;

namespace EventosPeruIA.CORE.Core.Entities;

public partial class VwResenasEvento
{
    public int? ResenaId { get; set; }

    public int? EventoId { get; set; }

    public int? UsuarioId { get; set; }

    public string? Autor { get; set; }

    public short? Calificacion { get; set; }

    public string? Comentario { get; set; }

    public DateTime? FechaCreacion { get; set; }
}

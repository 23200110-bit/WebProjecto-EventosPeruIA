using System;
using System.Collections.Generic;

namespace EventosPeruIA.CORE.Core.Entities;

public partial class VwPromedioResenasEvento
{
    public int? EventoId { get; set; }

    public int? TotalResenas { get; set; }

    public decimal? PromedioCalificacion { get; set; }
}

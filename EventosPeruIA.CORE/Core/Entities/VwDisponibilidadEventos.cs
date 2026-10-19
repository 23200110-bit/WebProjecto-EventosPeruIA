using System;
using System.Collections.Generic;

namespace EventosPeruIA.CORE.Core.Entities;

public partial class VwDisponibilidadEventos
{
    public int? EventoId { get; set; }

    public string? Nombre { get; set; }

    public int? Aforo { get; set; }

    public int? EntradasVendidas { get; set; }

    public int? CuposDisponibles { get; set; }
}

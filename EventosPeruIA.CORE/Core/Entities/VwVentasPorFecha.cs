using System;
using System.Collections.Generic;

namespace EventosPeruIA.CORE.Core.Entities;

public partial class VwVentasPorFecha
{
    public int? EventoId { get; set; }

    public int? OrganizadorId { get; set; }

    public DateOnly? Fecha { get; set; }

    public int? EntradasVendidasEseDia { get; set; }

    public decimal? IngresosEseDia { get; set; }
}

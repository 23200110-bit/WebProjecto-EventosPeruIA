using System;
using System.Collections.Generic;

namespace EventosPeruIA.CORE.Core.Entities;

public partial class VwAsistenciaPorEvento
{
    public int? EventoId { get; set; }

    public int? OrganizadorId { get; set; }

    public string? NombreEvento { get; set; }

    public int? EntradasVendidas { get; set; }

    public int? AsistentesIngresados { get; set; }

    public int? PendientesDeIngreso { get; set; }

    public int? EntradasAnuladas { get; set; }
}

using System;
using System.Collections.Generic;

namespace EventosPeruIA.CORE.Core.Entities;

public partial class VwVentasPorEvento
{
    public int? EventoId { get; set; }

    public int? OrganizadorId { get; set; }

    public string? NombreEvento { get; set; }

    public string? Estado { get; set; }

    public string? Moneda { get; set; }

    public int? Aforo { get; set; }

    public int? EntradasVendidas { get; set; }

    public int? EntradasDisponibles { get; set; }

    public decimal? IngresosConfirmados { get; set; }

    public int? EntradasAnuladas { get; set; }

    public decimal? MontoPendienteReembolso { get; set; }
}

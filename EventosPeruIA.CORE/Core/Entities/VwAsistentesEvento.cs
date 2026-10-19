using System;
using System.Collections.Generic;

namespace EventosPeruIA.CORE.Core.Entities;

public partial class VwAsistentesEvento
{
    public Guid? EntradaId { get; set; }

    public string? CodigoCorto { get; set; }

    public int? EventoId { get; set; }

    public int? OrganizadorId { get; set; }

    public int? CompradorId { get; set; }

    public string? Titular { get; set; }

    public string? Estado { get; set; }

    public string? EstadoReembolso { get; set; }

    public DateTime? FechaEmision { get; set; }

    public DateTime? FechaUso { get; set; }
}

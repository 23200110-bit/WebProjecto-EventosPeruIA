using System;
using System.Collections.Generic;

namespace EventosPeruIA.CORE.Core.Entities;

public partial class VwMisEntradas
{
    public Guid? EntradaId { get; set; }

    public string? CodigoCorto { get; set; }

    public int? CompradorId { get; set; }

    public string? Titular { get; set; }

    public string? Estado { get; set; }

    public string? EstadoReembolso { get; set; }

    public decimal? PrecioPagado { get; set; }

    public string? Moneda { get; set; }

    public DateTime? FechaEmision { get; set; }

    public DateTime? FechaUso { get; set; }

    public int? EventoId { get; set; }

    public string? Evento { get; set; }

    public string? NombreLugar { get; set; }

    public DateTime? FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }

    public string? Direccion { get; set; }

    public string? Ciudad { get; set; }

    public string? ImagenUrl { get; set; }

    public string? EstadoEvento { get; set; }

    public string? MotivoCancelacion { get; set; }

    public string? DetalleCancelacion { get; set; }
}

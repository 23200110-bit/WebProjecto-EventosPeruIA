using System;
using System.Collections.Generic;

namespace EventosPeruIA.CORE.Core.Entities;

/// <summary>
/// Entradas individuales con QR único (UUID). Estados: Valida, Utilizada, Anulada. Historial: no se borra.
/// </summary>
public partial class Entradas
{
    /// <summary>
    /// UUID generado por el servidor (RN-04). Es lo que contiene el código QR.
    /// </summary>
    public Guid EntradaId { get; set; }

    public int CompraId { get; set; }

    public int EventoId { get; set; }

    public int CompradorId { get; set; }

    /// <summary>
    /// Nombre del titular que se imprime en el voucher (copia del comprador al comprar).
    /// </summary>
    public string? TitularNombre { get; set; }

    public decimal PrecioPagado { get; set; }

    public string Estado { get; set; } = null!;

    /// <summary>
    /// PendienteReembolso solo en entradas Anuladas de eventos de pago.
    /// </summary>
    public string? EstadoReembolso { get; set; }

    public DateTime FechaEmision { get; set; }

    public DateTime? FechaUso { get; set; }

    public DateTime? FechaAnulacion { get; set; }

    public DateTime? FechaReembolso { get; set; }

    /// <summary>
    /// Referencia legible TCK-XXXXXXXXXX derivada del UUID; única por evento. Sirve para el ingreso manual.
    /// </summary>
    public string? CodigoCorto { get; set; }

    public virtual Checkins? Checkins { get; set; }

    public virtual Usuarios Comprador { get; set; } = null!;

    public virtual Compras Compras { get; set; } = null!;

    public virtual Eventos Evento { get; set; } = null!;

    public virtual ICollection<Resenas> Resenas { get; set; } = new List<Resenas>();
}

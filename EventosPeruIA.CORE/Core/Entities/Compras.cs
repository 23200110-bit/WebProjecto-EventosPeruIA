using System;
using System.Collections.Generic;

namespace EventosPeruIA.CORE.Core.Entities;

/// <summary>
/// Una compra agrupa una o más entradas de un mismo evento. Historial: no se borra.
/// </summary>
public partial class Compras
{
    public int CompraId { get; set; }

    public int CompradorId { get; set; }

    public int EventoId { get; set; }

    public int CantidadEntradas { get; set; }

    /// <summary>
    /// Precio del evento al momento de comprar; el monto_total siempre es precio_unitario x cantidad.
    /// </summary>
    public decimal PrecioUnitario { get; set; }

    public decimal MontoTotal { get; set; }

    public string Moneda { get; set; } = null!;

    /// <summary>
    /// Simulado mientras no exista pasarela de pagos real.
    /// </summary>
    public string MetodoPago { get; set; } = null!;

    public string? CodigoTransaccion { get; set; }

    public string Estado { get; set; } = null!;

    public DateTime FechaCompra { get; set; }

    public DateTime? FechaAnulacion { get; set; }

    public DateTime? FechaReembolso { get; set; }

    public virtual Usuarios Comprador { get; set; } = null!;

    public virtual ICollection<Entradas> Entradas { get; set; } = new List<Entradas>();

    public virtual Eventos Evento { get; set; } = null!;
}

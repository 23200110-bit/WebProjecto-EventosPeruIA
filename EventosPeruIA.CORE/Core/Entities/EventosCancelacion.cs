using System;
using System.Collections.Generic;

namespace EventosPeruIA.CORE.Core.Entities;

/// <summary>
/// Registro de la cancelación de un evento: quién, cuándo, por qué y cuántas entradas se anularon.
/// </summary>
public partial class EventosCancelacion
{
    public int EventoCancelacionId { get; set; }

    public int EventoId { get; set; }

    public int MotivoCancelacionId { get; set; }

    public string DescripcionAdicional { get; set; } = null!;

    public int CanceladoPorId { get; set; }

    public int CantidadEntradasAfectadas { get; set; }

    public decimal MontoReembolsable { get; set; }

    public DateTime FechaHora { get; set; }

    public virtual Usuarios CanceladoPor { get; set; } = null!;

    public virtual Eventos Evento { get; set; } = null!;

    public virtual MotivosCancelacion MotivoCancelacion { get; set; } = null!;
}

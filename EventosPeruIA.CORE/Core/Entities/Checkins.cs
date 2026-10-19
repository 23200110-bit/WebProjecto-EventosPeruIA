using System;
using System.Collections.Generic;
using System.Net;

namespace EventosPeruIA.CORE.Core.Entities;

/// <summary>
/// Cada intento de validación de una entrada el día del evento, aprobado o rechazado.
/// </summary>
public partial class Checkins
{
    public int CheckInId { get; set; }

    public Guid EntradaId { get; set; }

    public int EventoId { get; set; }

    public int ValidadoPorId { get; set; }

    public string MetodoIngreso { get; set; } = null!;

    public string Resultado { get; set; } = null!;

    /// <summary>
    /// Obligatorio cuando el resultado es Rechazado.
    /// </summary>
    public string? MotivoRechazo { get; set; }

    public string? Dispositivo { get; set; }

    public IPAddress? IpOrigen { get; set; }

    public DateTime FechaHora { get; set; }

    public virtual Entradas Entrada { get; set; } = null!;

    public virtual Eventos Evento { get; set; } = null!;

    public virtual Usuarios ValidadoPor { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace EventosPeruIA.CORE.Core.Entities;

/// <summary>
/// Reglas de negocio configurables (duraciones, tiempos, límites). Ver columna descripcion.
/// </summary>
public partial class ParametrosSistema
{
    public string Clave { get; set; } = null!;

    public string Valor { get; set; } = null!;

    public string Tipo { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public DateTime FechaActualizacion { get; set; }
}

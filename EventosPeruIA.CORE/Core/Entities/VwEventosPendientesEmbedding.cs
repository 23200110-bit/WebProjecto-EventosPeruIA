using System;
using System.Collections.Generic;

namespace EventosPeruIA.CORE.Core.Entities;

public partial class VwEventosPendientesEmbedding
{
    public int? EventoId { get; set; }

    public string? Texto { get; set; }

    public string? HashActual { get; set; }

    public int? EmbeddingVigenteId { get; set; }

    public string? HashVigente { get; set; }
}

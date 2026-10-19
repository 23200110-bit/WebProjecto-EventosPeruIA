using System;
using System.Collections.Generic;

namespace EventosPeruIA.CORE.Core.Entities;

public partial class VwEmbeddingsRecomendables
{
    public int? EmbeddingId { get; set; }

    public int? EventoId { get; set; }

    public string? ContenidoTexto { get; set; }

    public List<float>? VectorEmbedding { get; set; }

    public string? ModeloUsado { get; set; }

    public int? Version { get; set; }

    public DateTime? FechaGeneracion { get; set; }
}

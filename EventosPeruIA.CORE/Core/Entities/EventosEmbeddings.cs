using System;
using System.Collections.Generic;

namespace EventosPeruIA.CORE.Core.Entities;

/// <summary>
/// Vectores de los eventos para el chatbot (RAG). Un solo embedding vigente por evento; el resto es historial.
/// </summary>
public partial class EventosEmbeddings
{
    public int EmbeddingId { get; set; }

    public int EventoId { get; set; }

    public string ContenidoTexto { get; set; } = null!;

    /// <summary>
    /// md5 del texto indexado; si cambia el contenido del evento deja de coincidir y hay que reindexar.
    /// </summary>
    public string? ContenidoHash { get; set; }

    public List<float> VectorEmbedding { get; set; } = null!;

    public short? Dimensiones { get; set; }

    public string ModeloUsado { get; set; } = null!;

    public int Version { get; set; }

    /// <summary>
    /// Solo una versión vigente por evento (índice único parcial).
    /// </summary>
    public bool Vigente { get; set; }

    public DateTime FechaGeneracion { get; set; }

    public DateTime? FechaInvalidacion { get; set; }

    public virtual Eventos Evento { get; set; } = null!;
}

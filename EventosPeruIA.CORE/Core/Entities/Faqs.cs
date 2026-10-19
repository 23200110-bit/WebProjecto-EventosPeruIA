using System;
using System.Collections.Generic;

namespace EventosPeruIA.CORE.Core.Entities;

/// <summary>
/// Preguntas frecuentes: base de conocimiento del chatbot (RAG sobre FAQs).
/// </summary>
public partial class Faqs
{
    public int FaqId { get; set; }

    public string Pregunta { get; set; } = null!;

    public string Respuesta { get; set; } = null!;

    public string Categoria { get; set; } = null!;

    public int Orden { get; set; }

    public bool Activo { get; set; }

    public string? ContenidoHash { get; set; }

    public List<float>? VectorEmbedding { get; set; }

    public string? ModeloEmbedding { get; set; }

    public DateTime? FechaEmbedding { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaActualizacion { get; set; }
}

using System;
using System.Collections.Generic;

namespace EventosPeruIA.CORE.Core.Entities;

/// <summary>
/// Mensajes de una conversación, con las fuentes (eventos y FAQs) usadas para responder.
/// </summary>
public partial class MensajesChatbot
{
    public long MensajeId { get; set; }

    public int ConversacionId { get; set; }

    public string Emisor { get; set; } = null!;

    public string Contenido { get; set; } = null!;

    public List<int>? EventosReferenciados { get; set; }

    public List<int>? FaqsReferenciadas { get; set; }

    public string? ModeloUsado { get; set; }

    public int? TokensEntrada { get; set; }

    public int? TokensSalida { get; set; }

    public DateTime FechaHora { get; set; }

    public virtual ConversacionesChatbot Conversacion { get; set; } = null!;
}

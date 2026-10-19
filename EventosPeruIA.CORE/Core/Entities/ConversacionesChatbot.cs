using System;
using System.Collections.Generic;

namespace EventosPeruIA.CORE.Core.Entities;

/// <summary>
/// Conversaciones de un usuario con el asistente de IA (US-017).
/// </summary>
public partial class ConversacionesChatbot
{
    public int ConversacionId { get; set; }

    public int UsuarioId { get; set; }

    public string? Titulo { get; set; }

    public bool Activa { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime? UltimoMensajeEn { get; set; }

    public virtual ICollection<MensajesChatbot> MensajesChatbot { get; set; } = new List<MensajesChatbot>();

    public virtual Usuarios Usuario { get; set; } = null!;
}

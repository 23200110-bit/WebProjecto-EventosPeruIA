using System;
using System.Collections.Generic;
using System.Net;

namespace EventosPeruIA.CORE.Core.Entities;

/// <summary>
/// Tokens de un solo uso para recuperar contraseña (US-018). Solo se guarda el hash.
/// </summary>
public partial class TokensRecuperacionContrasena
{
    public Guid TokenId { get; set; }

    public int UsuarioId { get; set; }

    public string TokenHash { get; set; } = null!;

    public IPAddress? IpSolicitud { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime FechaExpiracion { get; set; }

    public bool Utilizado { get; set; }

    public DateTime? FechaUso { get; set; }

    public virtual Usuarios Usuario { get; set; } = null!;
}

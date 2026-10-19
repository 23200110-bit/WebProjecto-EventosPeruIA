using System;
using System.Collections.Generic;

namespace EventosPeruIA.CORE.Core.Entities;

/// <summary>
/// Cuentas del sistema. Toda cuenta puede comprar y organizar (RN-01); solo es_administrador es especial.
/// </summary>
public partial class Usuarios
{
    public int UsuarioId { get; set; }

    public string Nombres { get; set; } = null!;

    public string Apellidos { get; set; } = null!;

    public string Correo { get; set; } = null!;

    public bool CorreoVerificado { get; set; }

    public DateTime? FechaVerificacion { get; set; }

    public string? Telefono { get; set; }

    public string? FotoUrl { get; set; }

    /// <summary>
    /// Hash con sal (bcrypt/argon2/Identity). Nulo si la cuenta solo usa login con Google.
    /// </summary>
    public string? ContrasenaHash { get; set; }

    /// <summary>
    /// Local (correo y contraseña) o Google.
    /// </summary>
    public string ProveedorAuth { get; set; } = null!;

    /// <summary>
    /// Identificador del usuario en el proveedor externo (sub de Google).
    /// </summary>
    public string? IdExterno { get; set; }

    public bool EsAdministrador { get; set; }

    /// <summary>
    /// Activo, Bloqueado (por un administrador) o Inactivo.
    /// </summary>
    public string Estado { get; set; } = null!;

    public string? MotivoBloqueo { get; set; }

    public DateTime? FechaBloqueo { get; set; }

    public string? CiudadUbicacion { get; set; }

    public decimal? Latitud { get; set; }

    public decimal? Longitud { get; set; }

    /// <summary>
    /// Contador de fallos de inicio de sesión; lo gestiona fn_registrar_intento_login.
    /// </summary>
    public short IntentosFallidos { get; set; }

    /// <summary>
    /// Bloqueo temporal por fuerza bruta. Distinto del bloqueo administrativo (estado).
    /// </summary>
    public DateTime? BloqueadoHasta { get; set; }

    public DateTime? UltimoAcceso { get; set; }

    public DateTime FechaRegistro { get; set; }

    public DateTime? FechaActualizacion { get; set; }

    public virtual ICollection<AccionesModeracion> AccionesModeracion { get; set; } = new List<AccionesModeracion>();

    public virtual ICollection<Checkins> Checkins { get; set; } = new List<Checkins>();

    public virtual ICollection<Compras> Compras { get; set; } = new List<Compras>();

    public virtual ICollection<ConversacionesChatbot> ConversacionesChatbot { get; set; } = new List<ConversacionesChatbot>();

    public virtual ICollection<Entradas> Entradas { get; set; } = new List<Entradas>();

    public virtual ICollection<EventosCancelacion> EventosCancelacion { get; set; } = new List<EventosCancelacion>();

    public virtual ICollection<Eventos> EventosModeradoPor { get; set; } = new List<Eventos>();

    public virtual ICollection<Eventos> EventosOrganizador { get; set; } = new List<Eventos>();

    public virtual ICollection<Notificaciones> Notificaciones { get; set; } = new List<Notificaciones>();

    public virtual ICollection<Resenas> ResenasModeradaPor { get; set; } = new List<Resenas>();

    public virtual ICollection<Resenas> ResenasUsuario { get; set; } = new List<Resenas>();

    public virtual ICollection<TokensRecuperacionContrasena> TokensRecuperacionContrasena { get; set; } = new List<TokensRecuperacionContrasena>();

    public virtual ICollection<UsuarioEventoInteraccion> UsuarioEventoInteraccion { get; set; } = new List<UsuarioEventoInteraccion>();

    public virtual ICollection<UsuarioIntereses> UsuarioIntereses { get; set; } = new List<UsuarioIntereses>();
}

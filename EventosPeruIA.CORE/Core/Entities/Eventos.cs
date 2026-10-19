using System;
using System.Collections.Generic;
using NpgsqlTypes;

namespace EventosPeruIA.CORE.Core.Entities;

/// <summary>
/// Eventos publicados por organizadores. estado = ciclo de vida; estado_moderacion = decisión del administrador.
/// </summary>
public partial class Eventos
{
    public int EventoId { get; set; }

    public int OrganizadorId { get; set; }

    public int CategoriaId { get; set; }

    public string Nombre { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public string? ImagenUrl { get; set; }

    public string? NombreLugar { get; set; }

    public string Direccion { get; set; } = null!;

    public string Ciudad { get; set; } = null!;

    public decimal? Latitud { get; set; }

    public decimal? Longitud { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }

    public int Aforo { get; set; }

    public decimal Precio { get; set; }

    public string Moneda { get; set; } = null!;

    /// <summary>
    /// Tope de entradas por compra para este evento. Nulo = usa el parámetro global.
    /// </summary>
    public short? MaxEntradasPorCompra { get; set; }

    public bool? EsGratuito { get; set; }

    /// <summary>
    /// Borrador, Publicado, Finalizado o Cancelado (RN-06).
    /// </summary>
    public string Estado { get; set; } = null!;

    /// <summary>
    /// PendienteRevision, Aprobado, Rechazado, Suspendido o Retirado. Solo Aprobado se vende.
    /// </summary>
    public string EstadoModeracion { get; set; } = null!;

    public string? MotivoModeracion { get; set; }

    public int? ModeradoPorId { get; set; }

    public DateTime? FechaModeracion { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaPublicacion { get; set; }

    public DateTime? FechaActualizacion { get; set; }

    /// <summary>
    /// Se incrementa en cada UPDATE (concurrencia optimista en EF Core).
    /// </summary>
    public long VersionFila { get; set; }

    /// <summary>
    /// Índice de texto completo (español, sin tildes) sobre nombre y descripción.
    /// </summary>
    public NpgsqlTsVector? Busqueda { get; set; }

    public virtual Categorias Categoria { get; set; } = null!;

    public virtual ICollection<Checkins> Checkins { get; set; } = new List<Checkins>();

    public virtual ICollection<Compras> Compras { get; set; } = new List<Compras>();

    public virtual ICollection<Entradas> Entradas { get; set; } = new List<Entradas>();

    public virtual EventosCancelacion? EventosCancelacion { get; set; }

    public virtual EventosEmbeddings? EventosEmbeddings { get; set; }

    public virtual Usuarios? ModeradoPor { get; set; }

    public virtual ICollection<Notificaciones> Notificaciones { get; set; } = new List<Notificaciones>();

    public virtual Usuarios Organizador { get; set; } = null!;

    public virtual ICollection<Resenas> Resenas { get; set; } = new List<Resenas>();

    public virtual ICollection<UsuarioEventoInteraccion> UsuarioEventoInteraccion { get; set; } = new List<UsuarioEventoInteraccion>();
}

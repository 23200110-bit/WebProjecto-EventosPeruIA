using System;
using System.Collections.Generic;
using NpgsqlTypes;

namespace EventosPeruIA.CORE.Core.Entities;

public partial class VwEventosPublicos
{
    public int? EventoId { get; set; }

    public string? Nombre { get; set; }

    public string? Descripcion { get; set; }

    public string? ImagenUrl { get; set; }

    public int? CategoriaId { get; set; }

    public string? Categoria { get; set; }

    public string? NombreLugar { get; set; }

    public string? Direccion { get; set; }

    public string? Ciudad { get; set; }

    public decimal? Latitud { get; set; }

    public decimal? Longitud { get; set; }

    public DateTime? FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }

    public decimal? Precio { get; set; }

    public string? Moneda { get; set; }

    public bool? EsGratuito { get; set; }

    public short? MaxEntradasPorCompra { get; set; }

    public int? Aforo { get; set; }

    public int? EntradasVendidas { get; set; }

    public int? CuposDisponibles { get; set; }

    public bool? Agotado { get; set; }

    public int? OrganizadorId { get; set; }

    public string? Organizador { get; set; }

    public decimal? PromedioCalificacion { get; set; }

    public int? TotalResenas { get; set; }

    public decimal? PromedioOrganizador { get; set; }

    public int? TotalResenasOrganizador { get; set; }

    public NpgsqlTsVector? Busqueda { get; set; }
}

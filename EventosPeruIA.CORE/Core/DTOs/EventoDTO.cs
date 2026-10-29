using System.ComponentModel.DataAnnotations;

namespace EventosPeruIA.CORE.Core.DTOs;

public class EventoDTO
{
    public int EventoId { get; set; }
    public int OrganizadorId { get; set; }
    public int CategoriaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public string Direccion { get; set; } = string.Empty;
    public decimal? Latitud { get; set; }
    public decimal? Longitud { get; set; }
    public int Aforo { get; set; }
    public decimal Precio { get; set; }
    public string Estado { get; set; } = string.Empty;
}
public class EventoPatchDTO
{
    public string? ImagenUrl { get; set; }
    public string? NombreLugar { get; set; }

    [StringLength(150)]
    public string? Nombre { get; set; }
    public string? Descripcion { get; set; }

    [StringLength(100)]
    public string? Ciudad { get; set; }

    [Range(-90.0, 90.0)]
    public decimal? Latitud { get; set; }

    [Range(-180.0, 180.0)]
    public decimal? Longitud { get; set; }

    [Range(0.0, 99999999.99)]
    public decimal? Precio { get; set; }

    [Range(1, short.MaxValue)]
    public short? MaxEntradasPorCompra { get; set; }
    public bool? EsGratuito { get; set; }
}

public class EventoCreateDTO
{
    [Range(1, int.MaxValue, ErrorMessage = "La categoría es obligatoria.")]
    public int CategoriaId { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre no puede superar los 150 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "La descripción es obligatoria.")]
    public string Descripcion { get; set; } = string.Empty;

    public DateTime FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }

    [Required(ErrorMessage = "La dirección es obligatoria.")]
    [StringLength(250, ErrorMessage = "La dirección no puede superar los 250 caracteres.")]
    public string Direccion { get; set; } = string.Empty;

    [Required(ErrorMessage = "La ciudad es obligatoria.")]
    [StringLength(100, ErrorMessage = "La ciudad no puede superar los 100 caracteres.")]
    public string Ciudad { get; set; } = string.Empty;

    [Range(-90.0, 90.0, ErrorMessage = "La latitud debe estar entre -90 y 90.")]
    public decimal? Latitud { get; set; }

    [Range(-180.0, 180.0, ErrorMessage = "La longitud debe estar entre -180 y 180.")]
    public decimal? Longitud { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "El aforo debe ser un número entero mayor que cero.")]
    public int Aforo { get; set; }

    [Range(0.0, 99999999.99, ErrorMessage = "El precio no puede ser negativo.")]
    public decimal Precio { get; set; }

    public bool Publicar { get; set; }
}

public class EventoUpdateDTO
{
    public int EventoId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "La categoría es obligatoria.")]
    public int CategoriaId { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre no puede superar los 150 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "La descripción es obligatoria.")]
    public string Descripcion { get; set; } = string.Empty;

    public DateTime FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }

    [Required(ErrorMessage = "La dirección es obligatoria.")]
    [StringLength(250, ErrorMessage = "La dirección no puede superar los 250 caracteres.")]
    public string Direccion { get; set; } = string.Empty;

    [Required(ErrorMessage = "La ciudad es obligatoria.")]
    [StringLength(100, ErrorMessage = "La ciudad no puede superar los 100 caracteres.")]
    public string Ciudad { get; set; } = string.Empty;

    [Range(-90.0, 90.0, ErrorMessage = "La latitud debe estar entre -90 y 90.")]
    public decimal? Latitud { get; set; }

    [Range(-180.0, 180.0, ErrorMessage = "La longitud debe estar entre -180 y 180.")]
    public decimal? Longitud { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "El aforo debe ser un número entero mayor que cero.")]
    public int Aforo { get; set; }

    [Range(0.0, 99999999.99, ErrorMessage = "El precio no puede ser negativo.")]
    public decimal Precio { get; set; }

    public bool Publicar { get; set; }
}

public class EventoCancelDTO
{
    [Range(1, int.MaxValue, ErrorMessage = "El motivo de cancelación es obligatorio.")]
    public int MotivoCancelacionId { get; set; }

    [Required(ErrorMessage = "La descripción del motivo es obligatoria.")]
    [StringLength(500, ErrorMessage = "La descripción no puede superar los 500 caracteres.")]
    public string DescripcionAdicional { get; set; } = string.Empty;
}
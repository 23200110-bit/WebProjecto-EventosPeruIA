using System.ComponentModel.DataAnnotations;

namespace EventosPeruIA.CORE.Core.DTOs;

/// <summary>Imagen de un flyer (base64, sin prefijo data:) y las categorías entre las que se puede elegir.</summary>
public sealed class FlyerDraftRequestDTO
{
    [Required, StringLength(20)] public string MimeType { get; set; } = "";
    [Required, StringLength(8_000_000, MinimumLength = 100)] public string DataBase64 { get; set; } = "";
    [MaxLength(50)] public List<string> Categorias { get; set; } = [];
}

/// <summary>Borrador sugerido a partir del flyer. Todo campo no detectado llega en null: la persona lo completa y revisa.</summary>
public sealed record FlyerDraftDTO(
    string? Nombre,
    string? Descripcion,
    string? Categoria,
    string? FechaInicio,
    string? HoraInicio,
    string? FechaFin,
    string? HoraFin,
    string? Ciudad,
    string? NombreLugar,
    string? Direccion,
    decimal? Precio,
    int? Aforo,
    bool? AnioIndicado);

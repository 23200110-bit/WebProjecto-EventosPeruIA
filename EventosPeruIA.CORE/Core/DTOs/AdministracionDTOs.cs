using System.ComponentModel.DataAnnotations;

namespace EventosPeruIA.CORE.Core.DTOs;

public sealed class CambiarEstadoUsuarioDTO
{
    [Required, RegularExpression("^(Activo|Bloqueado)$")]
    public string Estado { get; set; } = "";
    [Required, StringLength(300, MinimumLength = 3)]
    public string Motivo { get; set; } = "";
    public bool Confirmado { get; set; }
}

public sealed class ModerarEventoDTO
{
    [Required, RegularExpression("^(Suspender|Retirar)$")]
    public string Accion { get; set; } = "";
    [Required, StringLength(300, MinimumLength = 3)]
    public string Motivo { get; set; } = "";
    public bool Confirmado { get; set; }
}

public record UsuarioAdminDTO(int UsuarioId, string Nombres, string Apellidos, string Correo, string Estado, bool EsAdministrador);
public record EventoAdminDTO(int EventoId, int OrganizadorId, string Nombre, string Estado, string EstadoModeracion);
public record AsistenteDTO(Guid EntradaId, string? CodigoCorto, int CompradorId, string Nombre, string Correo, string Estado, bool CheckInRealizado, DateTime? FechaCheckIn);
public record AsistentesResponseDTO(int EventoId, int EntradasVendidas, int CheckInsRealizados, int Validas, int Utilizadas, int Anuladas, int TotalResultados, int Pagina, int TamanoPagina, IReadOnlyList<AsistenteDTO> Asistentes);
public record PaginaDTO<T>(int Total, int Pagina, int TamanoPagina, IReadOnlyList<T> Resultados);

public sealed class ChatRequestDTO
{
    [Required, StringLength(2000, MinimumLength = 1)]
    public string Mensaje { get; set; } = "";
    [Range(1, int.MaxValue)]
    public int? ConversacionId { get; set; }
}
public record EventoChatDTO(int EventoId, string Nombre, string Descripcion, string Categoria, DateTime FechaInicio, DateTime? FechaFin, string Direccion, decimal Precio, int CuposDisponibles, string Moneda = "PEN")
{
    public string Url => $"/api/catalog/events/{EventoId}";
}
public record ChatResponseDTO(int ConversacionId, string Respuesta, IReadOnlyList<EventoChatDTO> Eventos);
public record TurnoChat(string Role, string Content);
public sealed class ProyectoException(int status, string message) : Exception(message)
{
    public int Status { get; } = status;
}

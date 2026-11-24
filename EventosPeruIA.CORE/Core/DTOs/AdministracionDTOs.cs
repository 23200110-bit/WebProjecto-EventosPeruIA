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
    [Required(ErrorMessage = "Escribe una pregunta."), StringLength(2000, MinimumLength = 1, ErrorMessage = "El mensaje debe tener entre 1 y 2000 caracteres.")]
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
public sealed class RagChatRequestDTO
{
    [Required, StringLength(2000, MinimumLength = 1)]
    public string Message { get; set; } = "";
}
public record RagSourceDTO(long Id, string Title, double Similarity);
public record RagChatResponseDTO(string Reply, IReadOnlyList<RagSourceDTO> Sources);
public sealed class BugReportRequestDTO
{
    [Required, StringLength(1500, MinimumLength = 20)]
    public string Description { get; set; } = "";
}
public record BugReportReceiptDTO(long ReportId, int DistinctReportersLast24Hours, bool AutomaticDiagnosisQueued);
public sealed class PendingBugIncidentDTO
{
    public string Fingerprint { get; set; } = "";
    public string Summary { get; set; } = "";
    public int ReportCount { get; set; }
    public int DistinctReporters { get; set; }
    public int AttemptCount { get; set; }
}
public sealed class BugReportEvidenceDTO
{
    public string Description { get; set; } = "";
    public bool ContainsPromptInjection { get; set; }
}
public sealed class PendingDocumentSectionDTO
{
    public long Id { get; set; }
    public string SourceType { get; set; } = "";
    public string SourceId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
}
public sealed class RagDocumentMatchDTO
{
    public long Id { get; set; }
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public double Similarity { get; set; }
}
public sealed class ProyectoException(int status, string message) : Exception(message)
{
    public int Status { get; } = status;
}

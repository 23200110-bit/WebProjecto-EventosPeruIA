using System;
using System.Collections.Generic;
using System.Text;

namespace EventosPeruIA.CORE.Core.DTOs.CheckIn
{
	// Parámetros enviados desde el escáner QR o terminal en puerta (US-012)
	public class ValidateCheckInRequestDTO
	{
		public int EventoId { get; set; }
		public string CodigoTicket { get; set; } = string.Empty; // UUID de la entrada o CodigoCorto
		public string MetodoIngreso { get; set; } = "QR";        // "QR" o "Manual"
		public int ValidadoPorId { get; set; }
		public string? Dispositivo { get; set; }
		public string? IpOrigen { get; set; }
	}

	// Respuesta tras la evaluación de acceso
	public class ValidateCheckInResponseDTO
	{
		public bool Success { get; set; }
		public string Message { get; set; } = string.Empty;
		public string? Estado { get; set; }
		public string? Titular { get; set; }
		public string? CodigoCorto { get; set; }
		public DateTime? FechaValidacion { get; set; }
	}
}


namespace EventosPeruIA.CORE.Core.DTOs.Entradas
{
    public class EntradaDto
    {
        public Guid EntradaId { get; set; }
        public int EventoId { get; set; }
        public string NombreEvento { get; set; } = string.Empty;
        public DateTime FechaEvento { get; set; }
        public string Ubicacion { get; set; } = string.Empty;
        public string NombreTitular { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string CodigoCorto { get; set; } = string.Empty;
        public string CodigoQrData { get; set; } = string.Empty;
        public bool QrDeshabilitado { get; set; }
        public string? MotivoCancelacion { get; set; }
        public bool PendienteReembolso { get; set; }
    }

    public class VoucherDetalleDto
    {
        public Guid EntradaId { get; set; }
        public string NombreEvento { get; set; } = string.Empty;
        public DateTime FechaYHora { get; set; }
        public string Ubicacion { get; set; } = string.Empty;
        public string NombreTitular { get; set; } = string.Empty;
        public decimal MontoPagado { get; set; }
        public string EstadoEntrada { get; set; } = string.Empty;
        public string CodigoCorto { get; set; } = string.Empty;
        public string QrPayload { get; set; } = string.Empty;
        public bool EstaActiva { get; set; }
        public string MensajeEstadoReembolso { get; set; } = string.Empty;
    }
}
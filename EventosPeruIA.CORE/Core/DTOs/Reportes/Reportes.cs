namespace EventosPeruIA.CORE.Core.DTOs.Reportes
{
    public class ReporteFiltroRequestDto
    {
        public int? EventoId { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
    }

    public class ResumenVentasEventoDto
    {
        public int EventoId { get; set; }
        public string NombreEvento { get; set; } = string.Empty;
        public int EntradasValidasVendidas { get; set; }
        public int EntradasUtilizadas { get; set; }
        public int TotalEntradasActivas => EntradasValidasVendidas + EntradasUtilizadas;
        public int CapacidadDisponible { get; set; }
        public decimal IngresosTotalesSoles { get; set; }
        public int EntradasAnuladasCount { get; set; }
        public decimal MontoPendienteReembolsoSoles { get; set; }
    }

    public class GraficoVentasDiariasDto
    {
        public DateTime Fecha { get; set; }
        public int EntradasVendidas { get; set; }
        public decimal MontoTotalSoles { get; set; }
    }

    public class ReporteDashboardOrganizadorDto
    {
        public DateTime FechaGeneracion { get; set; } = DateTime.UtcNow;
        public ReporteFiltroRequestDto FiltrosAplicados { get; set; } = new();
        public List<ResumenVentasEventoDto> ResumenEventos { get; set; } = new();
        public List<GraficoVentasDiariasDto> EvolucionVentasDiarias { get; set; } = new();
        public decimal GranTotalIngresosSoles { get; set; }
        public int GranTotalEntradasActivas { get; set; }
        public bool TieneDatos { get; set; }
        public string MensajeInformativo { get; set; } = string.Empty;
    }

    public class ArchivoExportadoResponseDto
    {
        public string NombreArchivo { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public byte[] ContenidoBytes { get; set; } = Array.Empty<byte>();
    }

    // Modelos internos para transferir datos puros desde el Repositorio hacia el Servicio
    public class DatosVentaEventoRaw
    {
        public int EventoId { get; set; }
        public string NombreEvento { get; set; } = string.Empty;
        public int EntradasDisponibles { get; set; }
    }

    public class DatosAsistenciaRaw
    {
        public int EventoId { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaEmision { get; set; }
    }

    public class DatosReembolsoRaw
    {
        public int EventoId { get; set; }
        public decimal PrecioPagado { get; set; }
        public DateTime FechaEmision { get; set; }
    }

    public class DatosVentaDiariaRaw
    {
        public int EventoId { get; set; }
        public DateOnly Fecha { get; set; }
        public int EntradasVendidas { get; set; }
        public decimal Ingresos { get; set; }
    }
}
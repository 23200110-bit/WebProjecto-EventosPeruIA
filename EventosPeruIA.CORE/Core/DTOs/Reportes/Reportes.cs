namespace EventosPeruIA.CORE.Core.DTOs.Reportes;

public class ReporteFiltroRequestDto
{
    public int? EventoId { get; set; }
    public DateOnly? FechaInicio { get; set; }
    public DateOnly? FechaFin { get; set; }
}

public class ResumenVentasEventoDto
{
    public int EventoId { get; set; }
    public string NombreEvento { get; set; } = string.Empty;
    public string EstadoEvento { get; set; } = string.Empty;
    public int EntradasValidasVendidas { get; set; }
    public int EntradasUtilizadas { get; set; }
    public int TotalEntradasActivas => EntradasValidasVendidas + EntradasUtilizadas;
    public int CapacidadDisponible { get; set; }
    public decimal IngresosTotalesSoles { get; set; }
    public decimal IngresosTotalesUsd { get; set; }
    public int EntradasAnuladasCount { get; set; }
    public int EntradasPendientesReembolso { get; set; }
    public decimal MontoPendienteReembolsoSoles { get; set; }
    public decimal MontoPendienteReembolsoUsd { get; set; }
}

public class GraficoVentasDiariasDto
{
    public DateOnly Fecha { get; set; }
    public int EntradasVendidas { get; set; }
    public decimal MontoTotalSoles { get; set; }
    public decimal MontoTotalUsd { get; set; }
}

public class ReporteDashboardOrganizadorDto
{
    public Guid ReporteId { get; set; }
    public DateTime FechaGeneracion { get; set; } = DateTime.UtcNow;
    public ReporteFiltroRequestDto FiltrosAplicados { get; set; } = new();
    public List<ResumenVentasEventoDto> ResumenEventos { get; set; } = new();
    public List<GraficoVentasDiariasDto> EvolucionVentasDiarias { get; set; } = new();
    public decimal GranTotalIngresosSoles { get; set; }
    public decimal GranTotalIngresosUsd { get; set; }
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

public record DatosReporteRaw(
    IReadOnlyList<DatosVentaEventoRaw> Eventos,
    IReadOnlyList<DatosVentaDiariaRaw> VentasDiarias);

public class DatosVentaEventoRaw
{
    public int EventoId { get; set; }
    public string NombreEvento { get; set; } = string.Empty;
    public string EstadoEvento { get; set; } = string.Empty;
    public int EntradasDisponibles { get; set; }
}

public class DatosVentaDiariaRaw
{
    public int EventoId { get; set; }
    public DateOnly Fecha { get; set; }
    public string Moneda { get; set; } = string.Empty;
    public int EntradasValidas { get; set; }
    public int EntradasUtilizadas { get; set; }
    public int EntradasAnuladas { get; set; }
    public int EntradasPendientesReembolso { get; set; }
    public decimal Ingresos { get; set; }
    public decimal MontoPendienteReembolso { get; set; }
    public int EntradasVendidas => EntradasValidas + EntradasUtilizadas;
}
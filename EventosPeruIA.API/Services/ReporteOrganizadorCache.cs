using EventosPeruIA.CORE.Core.DTOs.Reportes;

namespace EventosPeruIA.API.Services;

public sealed class ReporteOrganizadorCache(TimeProvider reloj)
{
    private const int Limite = 128;
    private static readonly TimeSpan Vigencia = TimeSpan.FromMinutes(10);
    private readonly object sincronizacion = new();
    private readonly Dictionary<(int OrganizadorId, Guid ReporteId),
        (ReporteDashboardOrganizadorDto Reporte, DateTimeOffset Vencimiento)> reportes = [];

    public void Guardar(int organizadorId, ReporteDashboardOrganizadorDto reporte)
    {
        lock (sincronizacion)
        {
            var ahora = reloj.GetUtcNow();
            foreach (var vencido in reportes.Where(entrada => entrada.Value.Vencimiento <= ahora).ToList())
                reportes.Remove(vencido.Key);

            if (reportes.Count >= Limite)
                reportes.Remove(reportes.MinBy(entrada => entrada.Value.Vencimiento).Key);

            reporte.ReporteId = Guid.NewGuid();
            reportes[(organizadorId, reporte.ReporteId)] = (reporte, ahora + Vigencia);
        }
    }

    public ReporteDashboardOrganizadorDto? Obtener(int organizadorId, Guid reporteId)
    {
        lock (sincronizacion)
        {
            var clave = (organizadorId, reporteId);
            if (!reportes.TryGetValue(clave, out var entrada))
                return null;

            if (entrada.Vencimiento > reloj.GetUtcNow())
                return entrada.Reporte;

            reportes.Remove(clave);
            return null;
        }
    }
}
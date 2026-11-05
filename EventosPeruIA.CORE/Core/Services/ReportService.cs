using System.ComponentModel.DataAnnotations;
using System.Text;
using EventosPeruIA.CORE.Core.DTOs.Reports;
using EventosPeruIA.CORE.Core.Interfaces;
using EventosPeruIA.CORE.Core.Interfaces.Repositories;
using EventosPeruIA.CORE.Core.Interfaces.Services;

namespace EventosPeruIA.CORE.Services
{
    public class ReportService : IReportService
    {
        private readonly IReportRepository _reportRepository;

        public ReportService(IReportRepository reportRepository)
        {
            _reportRepository = reportRepository;
        }

        public async Task<OrganizerDashboardReportDto> GetOrganizerDashboardReportAsync(Guid organizerId, ReportFilterRequestDto filter)
        {
            if (filter.StartDate.HasValue && filter.EndDate.HasValue && filter.StartDate.Value > filter.EndDate.Value)
            {
                throw new ValidationException("La fecha de inicio no puede ser posterior a la fecha de fin.");
            }

            if (filter.EventId.HasValue)
            {
                bool isOwner = await _reportRepository.IsOrganizerOfEventAsync(organizerId, filter.EventId.Value);
                if (!isOwner)
                {
                    throw new UnauthorizedAccessException("Acceso denegado: No tiene permisos para consultar datos de este evento.");
                }
            }

            var summaries = await _reportRepository.GetOrganizerEventSummariesAsync(organizerId, filter);
            var dailySales = await _reportRepository.GetOrganizerDailySalesAsync(organizerId, filter);

            bool hasData = summaries.Any() || dailySales.Any();

            return new OrganizerDashboardReportDto
            {
                GenerationDate = DateTime.UtcNow,
                AppliedFilters = filter,
                EventSummaries = summaries,
                DailySalesEvolution = dailySales,
                GrandTotalRevenueSoles = summaries.Sum(s => s.TotalRevenueSoles),
                GrandTotalActiveTickets = summaries.Sum(s => s.TotalActiveTickets),
                HasData = hasData,
                InformativeMessage = hasData ? string.Empty : "No existen ventas para el filtro elegido."
            };
        }

        public async Task<ExportFileResponseDto> ExportReportToExcelAsync(Guid organizerId, ReportFilterRequestDto filter)
        {
            var report = await GetOrganizerDashboardReportAsync(organizerId, filter);

            var sb = new StringBuilder();
            sb.AppendLine("Reporte de Ventas por Evento");
            sb.AppendLine($"Fecha de Generación: {report.GenerationDate:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();
            sb.AppendLine("Evento,Entradas Validas,Entradas Utilizadas,Capacidad Disponible,Ingresos (S/)");

            foreach (var item in report.EventSummaries)
            {
                sb.AppendLine($"\"{item.EventName}\",{item.ValidTicketsSold},{item.UsedTickets},{item.AvailableCapacity},{item.TotalRevenueSoles}");
            }

            return new ExportFileResponseDto
            {
                FileName = $"Reporte_Ventas_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv",
                ContentType = "text/csv",
                FileBytes = Encoding.UTF8.GetBytes(sb.ToString())
            };
        }

        public async Task<ExportFileResponseDto> ExportReportToPdfAsync(Guid organizerId, ReportFilterRequestDto filter)
        {
            var report = await GetOrganizerDashboardReportAsync(organizerId, filter);

            var sb = new StringBuilder();
            sb.AppendLine("=========================================");
            sb.AppendLine("         REPORTE DE VENTAS (PDF)        ");
            sb.AppendLine("=========================================");
            sb.AppendLine($"Fecha de Generacion: {report.GenerationDate:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Total Recaudado: S/ {report.GrandTotalRevenueSoles}");
            sb.AppendLine($"Total Entradas Activas: {report.GrandTotalActiveTickets}");
            sb.AppendLine("=========================================");

            return new ExportFileResponseDto
            {
                FileName = $"Reporte_Ventas_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf",
                ContentType = "application/pdf",
                FileBytes = Encoding.UTF8.GetBytes(sb.ToString())
            };
        }
    }
}
using System.ComponentModel.DataAnnotations;
using EventosPeruIA.CORE.Core.DTOs.Entradas;
using EventosPeruIA.CORE.Core.Entities;
using EventosPeruIA.CORE.Core.Interfaces;

namespace EventosPeruIA.CORE.Core.Services
{
    public class EntradaService : IEntradaService
    {
        private readonly IEntradaRepository _entradaRepository;

        public EntradaService(IEntradaRepository entradaRepository)
        {
            _entradaRepository = entradaRepository;
        }

        public async Task<List<EntradaDto>> GetMisEntradasAsync(int userId)
        {
            var entradas = await _entradaRepository.GetEntradasByUserIdAsync(userId);
            return entradas.Select(MapToEntradaDto).ToList();
        }

        public async Task<VoucherDetalleDto> GetVoucherByEntradaIdAsync(Guid entradaId, int userId)
        {
            var entrada = await _entradaRepository.GetEntradaByIdAsync(entradaId);

            if (entrada == null)
            {
                throw new KeyNotFoundException("La entrada especificada no existe.");
            }

            if (entrada.CompradorId != userId)
            {
                throw new UnauthorizedAccessException("Acceso denegado: No tienes permisos para consultar esta entrada.");
            }

            bool esValida = string.Equals(entrada.Estado, "Valida", StringComparison.OrdinalIgnoreCase)
                            && !string.Equals(entrada.Evento?.Estado, "Cancelado", StringComparison.OrdinalIgnoreCase);
            bool esAnulada = string.Equals(entrada.Estado, "Anulada", StringComparison.OrdinalIgnoreCase);
            bool pendienteReembolso = esAnulada
                && entrada.PrecioPagado > 0
                && string.Equals(entrada.EstadoReembolso, "PendienteReembolso", StringComparison.OrdinalIgnoreCase);

            string motivoCompleto = FormatearMotivoCancelacion(entrada.Evento?.EventosCancelacion);
            string mensajeReembolso = pendienteReembolso
                ? $"Pendiente de reembolso · Motivo: {motivoCompleto}"
                : string.Empty;

            return new VoucherDetalleDto
            {
                EntradaId = entrada.EntradaId,
                EventoId = entrada.EventoId,
                NombreEvento = entrada.Evento?.Nombre ?? string.Empty,
                ImagenUrl = entrada.Evento?.ImagenUrl,
                FechaYHora = entrada.Evento?.FechaInicio ?? DateTime.MinValue,
                Ciudad = entrada.Evento?.Ciudad ?? string.Empty,
                Ubicacion = FormatearUbicacion(entrada.Evento),
                NombreTitular = FormatearTitular(entrada),
                MontoPagado = entrada.PrecioPagado,
                EstadoEntrada = NormalizarEstado(entrada.Estado),
                CodigoCorto = entrada.CodigoCorto ?? string.Empty,
                QrPayload = esValida ? entrada.EntradaId.ToString() : string.Empty,
                EstaActiva = esValida,
                PendienteReembolso = pendienteReembolso,
                MotivoCancelacion = esAnulada ? motivoCompleto : null,
                MensajeEstadoReembolso = mensajeReembolso
            };
        }

        private static EntradaDto MapToEntradaDto(Entradas entrada)
        {
            bool esValida = string.Equals(entrada.Estado, "Valida", StringComparison.OrdinalIgnoreCase)
                            && !string.Equals(entrada.Evento?.Estado, "Cancelado", StringComparison.OrdinalIgnoreCase);
            bool esAnulada = string.Equals(entrada.Estado, "Anulada", StringComparison.OrdinalIgnoreCase);
            bool pendienteReembolso = esAnulada
                && entrada.PrecioPagado > 0
                && string.Equals(entrada.EstadoReembolso, "PendienteReembolso", StringComparison.OrdinalIgnoreCase);
            string motivoCompleto = FormatearMotivoCancelacion(entrada.Evento?.EventosCancelacion);

            return new EntradaDto
            {
                EntradaId = entrada.EntradaId,
                EventoId = entrada.EventoId,
                NombreEvento = entrada.Evento?.Nombre ?? string.Empty,
                ImagenUrl = entrada.Evento?.ImagenUrl,
                FechaEvento = entrada.Evento?.FechaInicio ?? DateTime.MinValue,
                Ciudad = entrada.Evento?.Ciudad ?? string.Empty,
                Ubicacion = FormatearUbicacion(entrada.Evento),
                NombreTitular = FormatearTitular(entrada),
                Precio = entrada.PrecioPagado,
                Estado = NormalizarEstado(entrada.Estado),
                EstadoEvento = entrada.Evento?.Estado ?? string.Empty,
                CodigoCorto = entrada.CodigoCorto ?? string.Empty,
                CodigoQrData = esValida ? entrada.EntradaId.ToString() : string.Empty,
                QrDeshabilitado = !esValida,
                MotivoCancelacion = esAnulada ? motivoCompleto : null,
                PendienteReembolso = pendienteReembolso
            };
        }

        private static string NormalizarEstado(string? estado)
        {
            if (string.Equals(estado, "Valida", StringComparison.OrdinalIgnoreCase)) return "Válida";
            if (string.Equals(estado, "Utilizada", StringComparison.OrdinalIgnoreCase)) return "Utilizada";
            if (string.Equals(estado, "Anulada", StringComparison.OrdinalIgnoreCase)) return "Anulada";
            return estado ?? "Válida";
        }

        private static string FormatearUbicacion(Eventos? evento)
        {
            if (evento == null) return string.Empty;
            if (string.IsNullOrWhiteSpace(evento.NombreLugar)) return evento.Direccion ?? string.Empty;
            if (string.IsNullOrWhiteSpace(evento.Direccion)) return evento.NombreLugar;
            return $"{evento.NombreLugar}, {evento.Direccion}";
        }

        private static string FormatearTitular(Entradas entrada)
        {
            if (!string.IsNullOrWhiteSpace(entrada.TitularNombre)) return entrada.TitularNombre;
            return $"{entrada.Comprador?.Nombres} {entrada.Comprador?.Apellidos}".Trim();
        }

        private static string FormatearMotivoCancelacion(EventosCancelacion? cancelacion)
        {
            if (cancelacion == null) return "Cancelación del evento por el organizador.";
            var nombreMotivo = cancelacion.MotivoCancelacion?.Descripcion;
            var descripcion = cancelacion.DescripcionAdicional;

            if (!string.IsNullOrWhiteSpace(nombreMotivo) && !string.IsNullOrWhiteSpace(descripcion))
                return $"{nombreMotivo}: {descripcion}";

            return descripcion ?? nombreMotivo ?? "Cancelación del evento por el organizador.";
        }
    }
}
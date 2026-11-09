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
                throw new ValidationException("La entrada especificada no existe.");
            }

            if (entrada.CompradorId != userId)
            {
                throw new UnauthorizedAccessException("Acceso denegado: No tienes permisos para consultar esta entrada.");
            }

            bool esValida = string.Equals(entrada.Estado, "Valida", StringComparison.OrdinalIgnoreCase);
            bool esAnulada = string.Equals(entrada.Estado, "Anulada", StringComparison.OrdinalIgnoreCase);

            string mensajeReembolso = esAnulada
                ? $"Anulada. Motivo: {entrada.Evento?.EventosCancelacion?.DescripcionAdicional ?? "Cancelación de evento"}. Pendiente de reembolso."
                : string.Empty;

            return new VoucherDetalleDto
            {
                EntradaId = entrada.EntradaId,
                NombreEvento = entrada.Evento?.Nombre ?? string.Empty,
                FechaYHora = entrada.Evento?.FechaInicio ?? DateTime.MinValue,
                Ubicacion = string.IsNullOrWhiteSpace(entrada.Evento?.NombreLugar)
                    ? entrada.Evento?.Direccion ?? string.Empty
                    : $"{entrada.Evento.NombreLugar} - {entrada.Evento.Direccion}",
                NombreTitular = string.IsNullOrWhiteSpace(entrada.TitularNombre)
                    ? $"{entrada.Comprador?.Nombres} {entrada.Comprador?.Apellidos}"
                    : entrada.TitularNombre,
                MontoPagado = entrada.PrecioPagado,
                EstadoEntrada = entrada.Estado,
                CodigoCorto = entrada.CodigoCorto ?? string.Empty,
                QrPayload = esValida ? entrada.EntradaId.ToString() : string.Empty,
                EstaActiva = esValida,
                MensajeEstadoReembolso = mensajeReembolso
            };
        }

        private static EntradaDto MapToEntradaDto(Entradas entrada)
        {
            bool esValida = string.Equals(entrada.Estado, "Valida", StringComparison.OrdinalIgnoreCase);
            bool esAnulada = string.Equals(entrada.Estado, "Anulada", StringComparison.OrdinalIgnoreCase);

            return new EntradaDto
            {
                EntradaId = entrada.EntradaId,
                EventoId = entrada.EventoId,
                NombreEvento = entrada.Evento?.Nombre ?? string.Empty,
                FechaEvento = entrada.Evento?.FechaInicio ?? DateTime.MinValue,
                Ubicacion = string.IsNullOrWhiteSpace(entrada.Evento?.NombreLugar)
                    ? entrada.Evento?.Direccion ?? string.Empty
                    : $"{entrada.Evento.NombreLugar} - {entrada.Evento.Direccion}",
                NombreTitular = string.IsNullOrWhiteSpace(entrada.TitularNombre)
                    ? $"{entrada.Comprador?.Nombres} {entrada.Comprador?.Apellidos}"
                    : entrada.TitularNombre,
                Precio = entrada.PrecioPagado,
                Estado = entrada.Estado,
                CodigoCorto = entrada.CodigoCorto ?? string.Empty,
                CodigoQrData = esValida ? entrada.EntradaId.ToString() : string.Empty,
                QrDeshabilitado = !esValida,
                MotivoCancelacion = entrada.Evento?.EventosCancelacion?.DescripcionAdicional,
                PendienteReembolso = esAnulada
            };
        }
    }
}
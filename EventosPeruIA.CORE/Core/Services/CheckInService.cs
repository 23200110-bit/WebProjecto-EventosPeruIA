using EventosPeruIA.CORE.Core.DTOs.CheckIn;
using EventosPeruIA.CORE.Core.Entities;
using EventosPeruIA.CORE.Core.Interfaces.Repositories;
using EventosPeruIA.CORE.Core.Interfaces.Services;

namespace EventosPeruIA.CORE.Core.Services;

public class CheckInService : ICheckInService
{
	private readonly ICheckInRepository _repository;

	public CheckInService(ICheckInRepository repository)
	{
		_repository = repository;
	}

	public async Task<ValidateCheckInResponseDTO> ValidateTicketAsync(ValidateCheckInRequestDTO request, CancellationToken ct = default)
	{
		// 1. Buscar la entrada por UUID o Código Corto
		var ticket = await _repository.GetTicketByCodeOrUuidAsync(request.EventoId, request.CodigoTicket);

		if (ticket == null)
		{
			await _repository.RegisterCheckInAsync(new Checkins
			{
				EventoId = request.EventoId,
				ValidadoPorId = request.ValidadoPorId,
				MetodoIngreso = request.MetodoIngreso,
				Resultado = "Rechazado",
				MotivoRechazo = "La entrada no existe o no pertenece a este evento.",
				Dispositivo = request.Dispositivo,
				FechaHora = DateTime.UtcNow
			});

			return new ValidateCheckInResponseDTO
			{
				Success = false,
				Message = "La entrada no existe o no pertenece a este evento."
			};
		}

		// 2. Validar si ya fue utilizada (Anti-fraude)
		if (ticket.Estado == "Utilizada")
		{
			await _repository.RegisterCheckInAsync(new Checkins
			{
				EntradaId = ticket.EntradaId,
				EventoId = request.EventoId,
				ValidadoPorId = request.ValidadoPorId,
				MetodoIngreso = request.MetodoIngreso,
				Resultado = "Rechazado",
				MotivoRechazo = "La entrada ya fue utilizada previamente.",
				Dispositivo = request.Dispositivo,
				FechaHora = DateTime.UtcNow
			});

			return new ValidateCheckInResponseDTO
			{
				Success = false,
				Message = "Acceso denegado: La entrada ya fue utilizada previamente.",
				Estado = ticket.Estado,
				Titular = ticket.TitularNombre,
				CodigoCorto = ticket.CodigoCorto
			};
		}

		// 3. Validar si está anulada
		if (ticket.Estado != "Valida")
		{
			await _repository.RegisterCheckInAsync(new Checkins
			{
				EntradaId = ticket.EntradaId,
				EventoId = request.EventoId,
				ValidadoPorId = request.ValidadoPorId,
				MetodoIngreso = request.MetodoIngreso,
				Resultado = "Rechazado",
				MotivoRechazo = $"Entrada no válida (Estado: {ticket.Estado}).",
				Dispositivo = request.Dispositivo,
				FechaHora = DateTime.UtcNow
			});

			return new ValidateCheckInResponseDTO
			{
				Success = false,
				Message = $"Acceso denegado: Entrada en estado '{ticket.Estado}'.",
				Estado = ticket.Estado,
				Titular = ticket.TitularNombre,
				CodigoCorto = ticket.CodigoCorto
			};
		}

		// 4. Check-in exitoso: marcar como Utilizada
		ticket.Estado = "Utilizada";
		ticket.FechaUso = DateTime.UtcNow;

		await _repository.RegisterCheckInAsync(new Checkins
		{
			EntradaId = ticket.EntradaId,
			EventoId = request.EventoId,
			ValidadoPorId = request.ValidadoPorId,
			MetodoIngreso = request.MetodoIngreso,
			Resultado = "Aprobado",
			Dispositivo = request.Dispositivo,
			FechaHora = DateTime.UtcNow
		});

		return new ValidateCheckInResponseDTO
		{
			Success = true,
			Message = "Check-in exitoso. Entrada validada correctamente.",
			Estado = ticket.Estado,
			Titular = ticket.TitularNombre,
			CodigoCorto = ticket.CodigoCorto,
			FechaValidacion = ticket.FechaUso
		};
	}
}
using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.EntityFrameworkCore;
using EventosPeruIA.CORE.Core.Entities;
using EventosPeruIA.CORE.Core.Interfaces.Repositories;
using EventosPeruIA.CORE.Infrastructure.Data;

namespace EventosPeruIA.CORE.Infrastructure.Repositories;

public class CheckInRepository : ICheckInRepository
{
	private readonly PostgresContext _context;

	public CheckInRepository(PostgresContext context)
	{
		_context = context;
	}

	public async Task<Entradas?> GetTicketByCodeOrUuidAsync(int eventId, string ticketCode)
	{
		Entradas? ticket = null;

		// 1. Evalúa si el código escaneado es un UUID (EntradaId)
		if (Guid.TryParse(ticketCode, out Guid ticketGuid))
		{
			ticket = await _context.Entradas
				.FirstOrDefaultAsync(t => t.EntradaId == ticketGuid && t.EventoId == eventId);
		}
		else
		{
			// 2. Si no es UUID, busca por el código legible corto (ej. TCK-XXXXXXXX)
			ticket = await _context.Entradas
				.FirstOrDefaultAsync(t => t.CodigoCorto == ticketCode && t.EventoId == eventId);
		}

		return ticket;
	}

	public async Task<bool> RegisterCheckInAsync(Checkins checkIn)
	{
		await _context.Checkins.AddAsync(checkIn);
		return await _context.SaveChangesAsync() > 0;
	}
}

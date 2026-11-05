using EventosPeruIA.CORE.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace EventosPeruIA.CORE.Core.Interfaces.Repositories
{
	public interface ICheckInRepository
	{
		Task<Entradas?> GetTicketByCodeOrUuidAsync(int eventId, string ticketCode);
		Task<bool> RegisterCheckInAsync(Checkins checkIn);

	}
}

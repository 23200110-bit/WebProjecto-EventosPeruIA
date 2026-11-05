using System;
using System.Collections.Generic;
using System.Text;

using EventosPeruIA.CORE.Core.DTOs.CheckIn;

namespace EventosPeruIA.CORE.Core.Interfaces.Services;

public interface ICheckInService
{
	Task<ValidateCheckInResponseDTO> ValidateTicketAsync(ValidateCheckInRequestDTO request, CancellationToken ct = default);
}
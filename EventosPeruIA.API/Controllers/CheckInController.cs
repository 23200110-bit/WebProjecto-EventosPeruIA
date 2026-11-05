using EventosPeruIA.CORE.Core.DTOs.CheckIn;
using EventosPeruIA.CORE.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventosPeruIA.API.Controllers;

[ApiController]
[Route("api/tickets")]
public class CheckInController : ControllerBase
{
	private readonly ICheckInService _checkInService;

	public CheckInController(ICheckInService checkInService)
	{
		_checkInService = checkInService;
	}

	// POST: /api/tickets/validate-qr (US-012)
	[HttpPost("validate-qr")]
	public async Task<IActionResult> ValidateQr([FromBody] ValidateCheckInRequestDTO request, CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(request.CodigoTicket))
		{
			return BadRequest(new { success = false, message = "El código o UUID de la entrada es obligatorio." });
		}

		var result = await _checkInService.ValidateTicketAsync(request, ct);

		if (!result.Success)
		{
			if (result.Message.Contains("no existe"))
			{
				return NotFound(result);
			}

			return BadRequest(result);
		}

		return Ok(result);
	}
}
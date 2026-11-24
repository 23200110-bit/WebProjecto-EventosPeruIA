using EventosPeruIA.API.Services;
using EventosPeruIA.CORE.Core.DTOs;
using EventosPeruIA.CORE.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EventosPeruIA.API.Controllers;

[ApiController, Authorize, Route("api/ai")]
public sealed class AiController(IAiService service, SystemBugReportService reports) : AuthenticatedControllerBase
{
    // Sin conversacionId inicia una nueva; con ID continúa una conversación propia.
    [HttpPost("chat"), EnableRateLimiting("chat")]
    public async Task<IActionResult> Chat([FromBody] ChatRequestDTO request, CancellationToken ct)
    {
        if (!TryGetUserId(out var usuarioId)) return Unauthorized();
        return Ok(await service.Chat(usuarioId, request, ct));
    }

    [HttpPost("bug-reports"), EnableRateLimiting("chat")]
    public async Task<ActionResult<BugReportReceiptDTO>> SubmitBugReport(
        [FromBody] BugReportRequestDTO request, CancellationToken ct)
    {
        if (!TryGetUserId(out var usuarioId)) return Unauthorized();
        return Ok(await reports.Submit(usuarioId, request.Description, ct));
    }
}

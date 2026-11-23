using EventosPeruIA.CORE.Core.DTOs;
using EventosPeruIA.CORE.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EventosPeruIA.API.Controllers;

[ApiController, AllowAnonymous, Route("api/chat")]
public sealed class PublicChatController(IRagChatService service) : ControllerBase
{
    [HttpPost, EnableRateLimiting("chat")]
    public async Task<ActionResult<RagChatResponseDTO>> Chat([FromBody] RagChatRequestDTO request, CancellationToken ct) =>
        Ok(await service.Chat(request.Message, ct));
}

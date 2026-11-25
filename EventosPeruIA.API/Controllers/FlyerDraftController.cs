using EventosPeruIA.CORE.Core.DTOs;
using EventosPeruIA.CORE.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EventosPeruIA.API.Controllers;

/// <summary>Sugiere un borrador de evento a partir de un flyer. No guarda nada: la persona lo revisa y usa POST /api/eventos.</summary>
[ApiController, Authorize, Route("api/eventos/flyer-borrador")]
public sealed class FlyerDraftController(IFlyerExtractor extractor) : ControllerBase
{
    [HttpPost, EnableRateLimiting("chat")]
    public async Task<ActionResult<FlyerDraftDTO>> Create([FromBody] FlyerDraftRequestDTO request, CancellationToken ct) =>
        Ok(await extractor.Extract(request, ct));
}

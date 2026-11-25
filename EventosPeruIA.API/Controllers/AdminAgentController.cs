using System.ComponentModel.DataAnnotations;
using EventosPeruIA.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EventosPeruIA.API.Controllers;

[ApiController, Authorize(Roles = "Admin"), Route("api/admin/agent")]
public sealed class AdminAgentController(AdminAgentService service) : ControllerBase
{
    [HttpPost, EnableRateLimiting("chat")]
    public async Task<ActionResult<AdminAgentResponse>> Run([FromBody] AdminAgentRequest request, CancellationToken ct)
    {
        var result = await service.Run(request.Prompt, ct);
        if (result.RestartManifest is { } manifest)
            Response.OnCompleted(() => service.SignalRestartAsync(manifest));
        return Ok(result);
    }
}

public sealed class AdminAgentRequest
{
    [Required, StringLength(8000, MinimumLength = 1)]
    public string Prompt { get; set; } = "";
}

using System.Net;
using System.Security.Cryptography;
using System.Text;
using EventosPeruIA.CORE.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventosPeruIA.API.Controllers;

[ApiController, AllowAnonymous, Route("internal/local-agent/restart-result")]
public sealed class LocalAgentRestartResultController(PostgresContext db, IConfiguration config) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Report([FromBody] RestartResultRequest request, CancellationToken ct)
    {
        if (Request.HttpContext.Connection.RemoteIpAddress is not { } address || !IPAddress.IsLoopback(address))
            return NotFound();

        var expected = config["AdminAgent:RestartToken"];
        var supplied = Request.Headers["X-Local-Agent-Token"].ToString();
        if (string.IsNullOrWhiteSpace(expected) ||
            !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(supplied), Encoding.UTF8.GetBytes(expected)))
            return Unauthorized();

        var status = request.Healthy ? "approved" : "rejected";
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var updated = await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE public.admin_code_patches
            SET status = {status}, validation_output = {request.Details}, original_content = NULL
            WHERE id = {request.PatchId} AND status = 'pending'
            """, ct);
        if (updated != 1) return NotFound();
        var incidentStatus = request.Healthy ? "resolved" : "rejected";
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE public.system_bug_incidents
            SET status = {incidentStatus}, validation_output = {request.Details}
            WHERE admin_code_patch_id = {request.PatchId}
              AND status = 'awaiting_restart'
            """, ct);
        await transaction.CommitAsync(ct);
        return NoContent();
    }
}

public sealed class RestartResultRequest
{
    public long PatchId { get; set; }
    public bool Healthy { get; set; }
    public string Details { get; set; } = "";
}

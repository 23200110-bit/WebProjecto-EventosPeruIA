using EventosPeruIA.CORE.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace EventosPeruIA.API.Controllers;
[ApiController,Authorize,Route("api/my")]
public class PersonalController : AuthenticatedControllerBase
{
 private readonly IPersonalService _service;
 public PersonalController(IPersonalService service){_service=service;}
 [HttpGet("tickets")]
 public async Task<IActionResult> Tickets(){if(!TryGetUserId(out var id))return Unauthorized();return Ok(await _service.GetTickets(id));}
 [HttpGet("notifications")]
 public async Task<IActionResult> Notifications(){if(!TryGetUserId(out var id))return Unauthorized();return Ok(await _service.GetNotifications(id));}
 [HttpPost("notifications/{notificationId:long}/read")]
 public async Task<IActionResult> Read(long notificationId){if(!TryGetUserId(out var id))return Unauthorized();return await _service.MarkRead(id,notificationId)?NoContent():NotFound();}
}

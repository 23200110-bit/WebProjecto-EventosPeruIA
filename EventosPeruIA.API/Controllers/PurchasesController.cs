using System.Security.Claims;
using EventosPeruIA.CORE.Core.DTOs;
using EventosPeruIA.CORE.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace EventosPeruIA.API.Controllers;
[ApiController,Authorize,Route("api/purchases")]
public sealed class PurchasesController(IPurchaseService service):ControllerBase {
 [HttpPost] public async Task<ActionResult<PurchaseResponse>> Buy(PurchaseRequest request,CancellationToken ct) {
  if(!int.TryParse(User.FindFirstValue("UserId"),out var uid))return Unauthorized();
  return Ok(await service.Buy(uid,request,ct));
 }
}

using System.Security.Claims;
using EventosPeruIA.CORE.Core.DTOs;
using EventosPeruIA.CORE.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace EventosPeruIA.API.Controllers;
[ApiController,Authorize,Route("api/purchases")]
public sealed class PurchasesController(IPurchaseService service,ITicketMailService mail,ILogger<PurchasesController> logger):ControllerBase {
 [HttpPost] public async Task<ActionResult<PurchaseResponse>> Buy(PurchaseRequest request,CancellationToken ct) {
  if(!int.TryParse(User.FindFirstValue("UserId"),out var uid))return Unauthorized();
  var purchase=await service.Buy(uid,request,ct);
  // El correo es un extra: si falla (SMTP caído o sin configurar) la compra ya está guardada y no se revierte.
  try { await mail.SendPurchaseAsync(uid,purchase,CancellationToken.None); }
  catch(Exception ex) { logger.LogWarning(ex,"No se pudo enviar el correo de la compra {CompraId}.",purchase.CompraId); }
  return Ok(purchase);
 }
}

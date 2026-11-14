using EventosPeruIA.CORE.Core.DTOs;
using EventosPeruIA.CORE.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
namespace EventosPeruIA.API.Controllers;
[ApiController,Route("api/auth"),EnableRateLimiting("password-reset")]
public sealed class PasswordResetController(IPasswordResetService service):ControllerBase {
 [HttpPost("forgot-password")] public async Task<IActionResult> Forgot(ForgotPasswordRequest request,CancellationToken ct){
  await service.Request(request,ct);return Accepted(new{mensaje="Si la cuenta permite recuperación, recibirás un enlace por correo."});
 }
 [HttpPost("reset-password")] public async Task<IActionResult> Reset(ResetPasswordRequest request,CancellationToken ct){
  await service.Reset(request,ct);return Ok(new{mensaje="Contraseña actualizada. Inicia sesión nuevamente."});
 }
}

using System.Security.Cryptography;
using System.Text;
using EventosPeruIA.CORE.Core.DTOs;
using EventosPeruIA.CORE.Core.Entities;
using EventosPeruIA.CORE.Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
namespace EventosPeruIA.CORE.Core.Services;
public sealed class PasswordResetService(IPasswordResetRepository repository,IPasswordResetMail mail,TimeProvider clock,IConfiguration config,ILogger<PasswordResetService> log, Microsoft.Extensions.Hosting.IHostEnvironment environment) : IPasswordResetService {
 public async Task Request(ForgotPasswordRequest request,CancellationToken ct) {
  Uri? url;
  if(!Uri.TryCreate(config["PasswordReset:PublicResetUrl"],UriKind.Absolute,out url)||!(url.Scheme=="https"||(environment.EnvironmentName=="Development"&&url.Scheme=="http"&&url.IsLoopback))||!string.IsNullOrEmpty(url.Fragment)||!string.IsNullOrEmpty(url.Query))
   throw new ProyectoException(503,"La recuperación por correo no está configurada.");
  try{mail.ValidateConfiguration();}catch(Exception ex)when(ex is InvalidOperationException or FormatException){throw new ProyectoException(503,"La recuperación por correo no está configurada.");}
  var email=request.Email.Trim().ToLowerInvariant();
  var user=await repository.FindUserByEmail(email,ct);
  if(user==null||user.Estado!="Activo"||user.ProveedorAuth!="Local"||user.ContrasenaHash==null)return;
  await using var tx=await repository.BeginTransaction(ct);
  user=await repository.LockUser(user.UsuarioId,ct);
  if(user==null||user.Estado!="Activo"||user.ProveedorAuth!="Local"||user.ContrasenaHash==null)return;
  var now=clock.GetUtcNow().UtcDateTime;
  if(await repository.HasRecentRequest(user.UsuarioId,now.AddMinutes(-1),ct))return;
  var raw=Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+','-').Replace('/','_');
  var token=new TokensRecuperacionContrasena{TokenId=Guid.NewGuid(),UsuarioId=user.UsuarioId,TokenHash=Hash(raw),FechaCreacion=now,FechaExpiracion=now.AddMinutes(20)};
  await repository.AddToken(token,ct);
  using var delivery=CancellationTokenSource.CreateLinkedTokenSource(ct);
  delivery.CancelAfter(TimeSpan.FromSeconds(15));
  try {await mail.Send(user.Correo,url.AbsoluteUri+"#token="+Uri.EscapeDataString(raw),delivery.Token);}
  catch(Exception)when(!ct.IsCancellationRequested){
   // Same public response for known/unknown accounts, no token/recipient/provider body in logs.
   log.LogWarning("No se pudo entregar un correo de recuperación; la solicitud se descartó.");return;
  }
  await tx.CommitAsync(ct);
 }
 public async Task Reset(ResetPasswordRequest request,CancellationToken ct) {
  if(request.Token.Length!=43||request.Token.Any(c=>!char.IsAsciiLetterOrDigit(c)&&c!='-'&&c!='_'))throw InvalidToken();
  var hash=Hash(request.Token);
  var uid=await repository.FindTokenUser(hash,ct);
  if(uid==null)throw InvalidToken();
  await using var tx=await repository.BeginTransaction(ct);
  var user=await repository.LockUser(uid.Value,ct);
  var token=await repository.LockToken(hash,ct);
  var now=clock.GetUtcNow().UtcDateTime;
  if(token==null||token.Utilizado||token.FechaExpiracion<=now||user==null||user.Estado!="Activo"||user.ProveedorAuth!="Local")throw InvalidToken();
  user.ContrasenaHash=new PasswordHasher<Usuarios>().HashPassword(user,request.Password);
  user.IntentosFallidos=0;user.BloqueadoHasta=null;user.FechaActualizacion=now;
  // Mark every outstanding token used atomically; no replay/parallel consumption.
  await repository.MarkAllTokensUsed(user.UsuarioId,now,ct);
  await repository.SaveChanges(ct);await tx.CommitAsync(ct);
 }
 public static string Hash(string value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
 static ProyectoException InvalidToken()=>new(400,"El enlace es inválido, expiró o ya fue utilizado.");
}

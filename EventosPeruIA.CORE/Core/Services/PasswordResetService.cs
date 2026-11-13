using System.Data;
using System.Security.Cryptography;
using System.Text;
using EventosPeruIA.CORE.Core.DTOs;
using EventosPeruIA.CORE.Core.Entities;
using EventosPeruIA.CORE.Infrastructure.Data;
using EventosPeruIA.CORE.Infrastructure.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
namespace EventosPeruIA.CORE.Core.Services;
public sealed class PasswordResetService(PostgresContext db,IPasswordResetMail mail,TimeProvider clock,IConfiguration config,ILogger<PasswordResetService> log) {
 public async Task Request(ForgotPasswordRequest request,CancellationToken ct) {
  Uri? url;
  if(!Uri.TryCreate(config["PasswordReset:PublicResetUrl"],UriKind.Absolute,out url)||url.Scheme!="https"||!string.IsNullOrEmpty(url.Fragment)||!string.IsNullOrEmpty(url.Query))
   throw new ProyectoException(503,"La recuperación por correo no está configurada.");
  try{mail.ValidateConfiguration();}catch(Exception ex)when(ex is InvalidOperationException or FormatException){throw new ProyectoException(503,"La recuperación por correo no está configurada.");}
  var email=request.Email.Trim().ToLowerInvariant();
  var user=await db.Usuarios.AsNoTracking().SingleOrDefaultAsync(u=>u.Correo==email,ct);
  if(user==null||user.Estado!="Activo"||user.ProveedorAuth!="Local"||user.ContrasenaHash==null)return;
  await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted,ct);
  user=await db.Usuarios.FromSqlInterpolated($"SELECT * FROM usuarios WHERE usuario_id={user.UsuarioId} FOR UPDATE").SingleAsync(ct);
  if(user.Estado!="Activo"||user.ProveedorAuth!="Local"||user.ContrasenaHash==null)return;
  var now=clock.GetUtcNow().UtcDateTime;
  if(await db.TokensRecuperacionContrasena.AnyAsync(t=>t.UsuarioId==user.UsuarioId&&t.FechaCreacion>now.AddMinutes(-1),ct))return;
  var raw=Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+','-').Replace('/','_');
  var token=new TokensRecuperacionContrasena{TokenId=Guid.NewGuid(),UsuarioId=user.UsuarioId,TokenHash=Hash(raw),FechaCreacion=now,FechaExpiracion=now.AddMinutes(20)};
  db.TokensRecuperacionContrasena.Add(token);await db.SaveChangesAsync(ct);
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
  var uid=await db.TokensRecuperacionContrasena.AsNoTracking().Where(t=>t.TokenHash==hash).Select(t=>(int?)t.UsuarioId).SingleOrDefaultAsync(ct);
  if(uid==null)throw InvalidToken();
  await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted,ct);
  var user=await db.Usuarios.FromSqlInterpolated($"SELECT * FROM usuarios WHERE usuario_id={uid.Value} FOR UPDATE").SingleOrDefaultAsync(ct);
  var token=await db.TokensRecuperacionContrasena.FromSqlInterpolated($"SELECT * FROM tokens_recuperacion_contrasena WHERE token_hash={hash} FOR UPDATE").SingleOrDefaultAsync(ct);
  var now=clock.GetUtcNow().UtcDateTime;
  if(token==null||token.Utilizado||token.FechaExpiracion<=now||user==null||user.Estado!="Activo"||user.ProveedorAuth!="Local")throw InvalidToken();
  user.ContrasenaHash=new PasswordHasher<Usuarios>().HashPassword(user,request.Password);
  user.IntentosFallidos=0;user.BloqueadoHasta=null;user.FechaActualizacion=now;
  // Mark every outstanding token used atomically; no replay/parallel consumption.
  await db.TokensRecuperacionContrasena.Where(t=>t.UsuarioId==user.UsuarioId&&!t.Utilizado).ExecuteUpdateAsync(s=>s.SetProperty(t=>t.Utilizado,true).SetProperty(t=>t.FechaUso,now),ct);
  await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
 }
 public static string Hash(string value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
 static ProyectoException InvalidToken()=>new(400,"El enlace es inválido, expiró o ya fue utilizado.");
}

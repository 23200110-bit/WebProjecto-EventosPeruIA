using EventosPeruIA.CORE.Core.Interfaces;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
namespace EventosPeruIA.CORE.Infrastructure.Shared;
public sealed class SmtpPasswordResetMail(IConfiguration config):IPasswordResetMail {
 public void ValidateConfiguration() {
  if(string.IsNullOrWhiteSpace(config["PasswordReset:Smtp:Host"])||string.IsNullOrWhiteSpace(config["PasswordReset:Smtp:From"]))
   throw new InvalidOperationException("Configura el correo SMTP de recuperación.");
  _=new MailAddress(config["PasswordReset:Smtp:From"]!);
 }
 public async Task Send(string recipient,string resetUrl,CancellationToken ct) {
  ValidateConfiguration();
  using var smtp=new SmtpClient(config["PasswordReset:Smtp:Host"],config.GetValue<int>("PasswordReset:Smtp:Port",587)){
   EnableSsl=true,UseDefaultCredentials=false,Timeout=15000};
  var username=config["PasswordReset:Smtp:Username"];
  if(!string.IsNullOrWhiteSpace(username))smtp.Credentials=new NetworkCredential(username,config["PasswordReset:Smtp:Password"]);
  using var message=new MailMessage(config["PasswordReset:Smtp:From"]!,recipient){Subject="Recuperar contraseña — EventosPeruIA",
   Body=$"Solicitaste restablecer tu contraseña. El enlace vence en 20 minutos y es de un solo uso.\n\n{resetUrl}\n\nSi no lo solicitaste, ignora este correo.",IsBodyHtml=false};
  await smtp.SendMailAsync(message,ct);
 }
}

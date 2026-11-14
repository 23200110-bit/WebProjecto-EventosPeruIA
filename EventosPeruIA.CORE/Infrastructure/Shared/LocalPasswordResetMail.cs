using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
namespace EventosPeruIA.CORE.Infrastructure.Shared;
/// <summary>Private local mailbox for development only; never exposed through HTTP.</summary>
public sealed class LocalPasswordResetMail(IConfiguration config,IHostEnvironment environment):IPasswordResetMail {
 public static string DefaultDirectory=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"EventosPeruIA","password-mail");
 string DirectoryPath=>config["PasswordReset:PickupDirectory"]??DefaultDirectory;
 public void ValidateConfiguration(){
  if(environment.EnvironmentName!="Development"||!Path.IsPathFullyQualified(DirectoryPath))
   throw new InvalidOperationException("El buzón local sólo está habilitado en Development con ruta absoluta.");
 }
 public async Task Send(string recipient,string resetUrl,CancellationToken ct){
  ValidateConfiguration();
  var directory=DirectoryPath;
  if(Directory.Exists(directory)&&(File.GetAttributes(directory)&FileAttributes.ReparsePoint)!=0)
   throw new InvalidOperationException("El buzón local no admite enlaces simbólicos.");
  if(OperatingSystem.IsWindows())Directory.CreateDirectory(directory);
  else {Directory.CreateDirectory(directory,UnixFileMode.UserRead|UnixFileMode.UserWrite|UnixFileMode.UserExecute);
   File.SetUnixFileMode(directory,UnixFileMode.UserRead|UnixFileMode.UserWrite|UnixFileMode.UserExecute);}
  var address=new MailAddress(recipient).Address;
  var body=$"Solicitaste restablecer tu contraseña. El enlace vence en 20 minutos y es de un solo uso.\n\n{resetUrl}\n\nSi no lo solicitaste, ignora este correo.";
  var mime=$"From: EventosPeruIA <no-reply@eventosperuia.test>\r\nTo: {address}\r\nDate: {DateTimeOffset.UtcNow:r}\r\nMessage-ID: <{Guid.NewGuid():N}@eventosperuia.test>\r\nSubject: Recuperar password - EventosPeruIA\r\nMIME-Version: 1.0\r\nContent-Type: text/plain; charset=utf-8\r\nContent-Transfer-Encoding: base64\r\n\r\n"+Convert.ToBase64String(Encoding.UTF8.GetBytes(body),Base64FormattingOptions.InsertLineBreaks)+"\r\n";
  var options=new FileStreamOptions{Mode=FileMode.CreateNew,Access=FileAccess.Write,Share=FileShare.None,Options=FileOptions.Asynchronous};
  if(!OperatingSystem.IsWindows())options.UnixCreateMode=UnixFileMode.UserRead|UnixFileMode.UserWrite;
  var path=Path.Combine(directory,Guid.NewGuid().ToString("N")+".eml");
  try {await using var file=new FileStream(path,options);await file.WriteAsync(Encoding.UTF8.GetBytes(mime),ct);}
  catch {File.Delete(path);throw;}
 }
}

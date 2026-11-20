using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using EventosPeruIA.CORE.Core.Interfaces;
using Microsoft.Extensions.Configuration;
namespace EventosPeruIA.CORE.Infrastructure.Shared;

/// <summary>Correo real por SMTP (Gmail con contraseña de aplicación, Brevo, etc.). Usa Email:Smtp y, si falta, PasswordReset:Smtp.</summary>
public sealed class SmtpEmailSender(IConfiguration config) : IEmailSender
{
    private string? Setting(string key) => config["Email:Smtp:" + key] ?? config["PasswordReset:Smtp:" + key];

    public async Task SendAsync(string recipient, string subject, string htmlBody, IReadOnlyList<InlineImage>? images, CancellationToken ct)
    {
        var host = Setting("Host");
        var from = Setting("From");
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
            throw new InvalidOperationException("Configura el correo SMTP (Email:Smtp:Host y Email:Smtp:From).");
        var port = int.TryParse(Setting("Port"), out var p) ? p : 587;

        using var smtp = new SmtpClient(host, port) { EnableSsl = true, UseDefaultCredentials = false, Timeout = 15000 };
        var username = Setting("Username");
        if (!string.IsNullOrWhiteSpace(username))
            smtp.Credentials = new NetworkCredential(username, Setting("Password"));

        using var message = new MailMessage(from, recipient)
        {
            Subject = subject,
            SubjectEncoding = Encoding.UTF8,
            BodyEncoding = Encoding.UTF8
        };
        using var view = AlternateView.CreateAlternateViewFromString(htmlBody, Encoding.UTF8, MediaTypeNames.Text.Html);
        var streams = new List<MemoryStream>();
        try
        {
            foreach (var image in images ?? [])
            {
                var stream = new MemoryStream(image.Content);
                streams.Add(stream);
                view.LinkedResources.Add(new LinkedResource(stream, image.MediaType)
                {
                    ContentId = image.ContentId,
                    TransferEncoding = TransferEncoding.Base64
                });
            }
            message.AlternateViews.Add(view);
            await smtp.SendMailAsync(message, ct);
        }
        finally
        {
            foreach (var stream in streams) stream.Dispose();
        }
    }
}

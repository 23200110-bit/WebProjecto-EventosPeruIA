namespace EventosPeruIA.CORE.Core.Interfaces;

/// <summary>Imagen incrustada en el cuerpo HTML; se referencia como src="cid:ContentId".</summary>
public sealed record InlineImage(string ContentId, byte[] Content, string MediaType = "image/png");

/// <summary>Envío de correo genérico (entradas, avisos). Recuperación de contraseña conserva IPasswordResetMail.</summary>
public interface IEmailSender
{
    Task SendAsync(string recipient, string subject, string htmlBody, IReadOnlyList<InlineImage>? images, CancellationToken ct);
}

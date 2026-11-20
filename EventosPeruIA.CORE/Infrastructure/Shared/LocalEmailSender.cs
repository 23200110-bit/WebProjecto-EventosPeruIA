using System.Text;
using EventosPeruIA.CORE.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
namespace EventosPeruIA.CORE.Infrastructure.Shared;

/// <summary>Solo Development: guarda cada correo como .html (imágenes incrustadas) para abrirlo en el navegador. No envía nada a Internet.</summary>
public sealed class LocalEmailSender(IConfiguration config, IHostEnvironment environment) : IEmailSender
{
    public static string DefaultDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EventosPeruIA", "mail");

    public async Task SendAsync(string recipient, string subject, string htmlBody, IReadOnlyList<InlineImage>? images, CancellationToken ct)
    {
        var directory = config["Email:PickupDirectory"] ?? DefaultDirectory;
        if (environment.EnvironmentName != "Development" || !Path.IsPathFullyQualified(directory))
            throw new InvalidOperationException("El buzón local solo está habilitado en Development con ruta absoluta. Usa Email:DeliveryMode=Smtp.");
        Directory.CreateDirectory(directory);

        var html = htmlBody;
        foreach (var image in images ?? [])
            html = html.Replace("cid:" + image.ContentId, $"data:{image.MediaType};base64,{Convert.ToBase64String(image.Content)}");

        var header = $"<!-- Para: {System.Net.WebUtility.HtmlEncode(recipient)} | Asunto: {System.Net.WebUtility.HtmlEncode(subject)} -->\n";
        var path = Path.Combine(directory, $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.html");
        await File.WriteAllTextAsync(path, header + html, Encoding.UTF8, ct);
    }
}

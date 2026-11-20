using System.Net;
using EventosPeruIA.CORE.Core.DTOs;
using EventosPeruIA.CORE.Core.Interfaces;
using EventosPeruIA.CORE.Core.Interfaces.Repositories;
using QRCoder;
namespace EventosPeruIA.CORE.Core.Services;

public sealed class TicketMailService(IAccountRepository accounts, IEventoRepository eventos, IEmailSender email) : ITicketMailService
{
    public async Task SendPurchaseAsync(int userId, PurchaseResponse purchase, CancellationToken ct)
    {
        var user = await accounts.FindByIdAsync(userId, ct);
        var evento = await eventos.GetEventoByIdAsync(purchase.EventoId);
        if (user is null || evento is null || purchase.Entradas.Count == 0) return;

        var images = new List<InlineImage>();
        var tickets = new System.Text.StringBuilder();
        for (var i = 0; i < purchase.Entradas.Count; i++)
        {
            var ticket = purchase.Entradas[i];
            var cid = $"qr{i + 1}";
            images.Add(new InlineImage(cid, Qr(ticket.QrPayload)));
            tickets.Append($"""
                <tr><td style="padding:12px;border:1px solid #e3e0ef;border-radius:12px;text-align:center">
                <div style="font-weight:700;margin-bottom:6px">Entrada {i + 1}</div>
                <img src="cid:{cid}" width="180" height="180" alt="QR de la entrada {i + 1}" />
                <div style="margin-top:6px;font-size:18px;letter-spacing:2px">{WebUtility.HtmlEncode(ticket.CodigoCorto)}</div>
                </td></tr>
                """);
        }

        var lugar = string.Join(", ", new[] { evento.NombreLugar, evento.Direccion, evento.Ciudad }.Where(s => !string.IsNullOrWhiteSpace(s)));
        var html = $"""
            <div style="font-family:Arial,sans-serif;max-width:520px;margin:auto;color:#231b3c">
            <h2 style="margin-bottom:4px">¡Tu compra está lista, {WebUtility.HtmlEncode(user.Nombres)}!</h2>
            <p style="margin-top:0">{WebUtility.HtmlEncode(evento.Nombre)}</p>
            <p><b>Fecha:</b> {evento.FechaInicio:dd/MM/yyyy HH:mm}<br/><b>Lugar:</b> {WebUtility.HtmlEncode(lugar)}<br/>
            <b>Entradas:</b> {purchase.Cantidad} &middot; <b>Total:</b> {purchase.MontoTotal:0.00} {WebUtility.HtmlEncode(purchase.Moneda)}</p>
            <p>Presenta el QR de cada entrada en la puerta del evento. El pago es simulado en esta demostración.</p>
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="border-spacing:0 10px">{tickets}</table>
            <p style="font-size:12px;color:#6b6585">EventosPerúIA</p>
            </div>
            """;

        await email.SendAsync(user.Correo, $"Tus entradas para {evento.Nombre}", html, images, ct);
    }

    private static byte[] Qr(string payload)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        return new PngByteQRCode(data).GetGraphic(8);
    }
}

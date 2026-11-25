using System.Net;
using EventosPeruIA.CORE.Core.Entities;
using EventosPeruIA.CORE.Core.Interfaces;
using EventosPeruIA.CORE.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EventosPeruIA.CORE.Infrastructure.Shared;

public sealed class AvisoEventoService(PostgresContext context, IEmailSender email, ILogger<AvisoEventoService> logger) : IAvisoEventoService
{
    // Tipo ya usado por el sistema para avisos de cambios; el tipo concreto se ve en el título.
    private const string TipoAviso = "CambioEvento";

    public async Task<List<int>> CompradoresValidosAsync(int eventoId, CancellationToken ct = default) =>
        await context.Entradas.AsNoTracking()
            .Where(x => x.EventoId == eventoId && x.Estado == "Valida")
            .Select(x => x.CompradorId).Distinct().ToListAsync(ct);

    public async Task AvisarAsync(int? eventoId, IReadOnlyCollection<int> usuarioIds, string titulo, string mensaje,
        DateTime? omitirPantallaSiAvisadosDesde = null, CancellationToken ct = default)
    {
        if (usuarioIds.Count == 0) return;
        var ids = usuarioIds.Distinct().ToList();
        var tituloFinal = Truncar(titulo, 150);
        var mensajeFinal = Truncar(mensaje, 500);

        try
        {
            var yaAvisados = new HashSet<int>();
            if (omitirPantallaSiAvisadosDesde is { } desde && eventoId.HasValue)
            {
                yaAvisados = (await context.Notificaciones.AsNoTracking()
                    .Where(n => n.EventoId == eventoId && n.FechaCreacion >= desde && ids.Contains(n.UsuarioId))
                    .Select(n => n.UsuarioId).Distinct().ToListAsync(ct)).ToHashSet();
            }
            var nuevas = ids.Where(id => !yaAvisados.Contains(id)).Select(id => new Notificaciones
            {
                UsuarioId = id, EventoId = eventoId, Tipo = TipoAviso, Titulo = tituloFinal, Mensaje = mensajeFinal
            }).ToList();
            if (nuevas.Count > 0)
            {
                await context.Notificaciones.AddRangeAsync(nuevas, ct);
                await context.SaveChangesAsync(ct);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudieron crear las notificaciones del aviso '{Titulo}'.", tituloFinal);
        }

        List<Usuarios> destinatarios;
        try
        {
            destinatarios = await context.Usuarios.AsNoTracking().Where(u => ids.Contains(u.UsuarioId)).ToListAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudieron leer los destinatarios del aviso '{Titulo}'.", tituloFinal);
            return;
        }

        foreach (var user in destinatarios)
        {
            if (string.IsNullOrWhiteSpace(user.Correo)) continue;
            var html = $"""
                <div style="font-family:Arial,sans-serif;max-width:520px;margin:auto;color:#231b3c">
                <h2 style="margin-bottom:4px">{WebUtility.HtmlEncode(tituloFinal)}</h2>
                <p>Hola {WebUtility.HtmlEncode(user.Nombres)},</p>
                <p>{WebUtility.HtmlEncode(mensaje).Replace("\n", "<br/>")}</p>
                <p style="font-size:12px;color:#6b6585">EventosPerúIA &middot; También puedes verlo en tus notificaciones.</p>
                </div>
                """;
            try { await email.SendAsync(user.Correo, tituloFinal, html, null, CancellationToken.None); }
            catch (Exception ex) { logger.LogWarning(ex, "No se pudo enviar el correo del aviso '{Titulo}' al usuario {UsuarioId}.", tituloFinal, user.UsuarioId); }
        }
    }

    private static string Truncar(string text, int max) => text.Length <= max ? text : text[..(max - 1)] + "…";
}

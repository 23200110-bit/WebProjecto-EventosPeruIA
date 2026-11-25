namespace EventosPeruIA.CORE.Core.Interfaces;

/// <summary>Avisos a usuarios: crea la notificación en pantalla y envía el mismo texto por correo.</summary>
public interface IAvisoEventoService
{
    /// <summary>Usuarios con al menos una entrada válida del evento (llamar antes de cancelar, que anula las entradas).</summary>
    Task<List<int>> CompradoresValidosAsync(int eventoId, CancellationToken ct = default);

    /// <summary>
    /// Avisa a cada usuario (pantalla + correo). Si <paramref name="omitirPantallaSiAvisadosDesde"/> tiene valor,
    /// no duplica la notificación a quien ya recibió una del mismo evento desde esa fecha; el correo sí se envía.
    /// Nunca lanza: un fallo de correo o de notificación se registra y no revierte la operación principal.
    /// </summary>
    Task AvisarAsync(int? eventoId, IReadOnlyCollection<int> usuarioIds, string titulo, string mensaje,
        DateTime? omitirPantallaSiAvisadosDesde = null, CancellationToken ct = default);
}

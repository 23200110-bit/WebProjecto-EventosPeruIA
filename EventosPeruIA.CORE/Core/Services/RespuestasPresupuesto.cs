using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using EventosPeruIA.CORE.Core.DTOs;

namespace EventosPeruIA.CORE.Core.Services;

internal static class RespuestasPresupuesto
{
    internal sealed record Presupuesto(decimal Monto, string? Moneda);

    internal static bool EsConsulta(string mensaje) => Regex.IsMatch(Normalizar(mensaje),
        @"\b(presupuesto|alcanza|alcanzan|maximo|puedo pagar|solo tengo|cuanto falta|cuanto sobra)\b") || Extraer(mensaje) != null;

    internal static Presupuesto? Ultimo(IEnumerable<TurnoChat> turnos) => turnos.Where(t => t.Role == "user")
        .Reverse().Select(t => Extraer(t.Content)).FirstOrDefault(p => p != null);

    private static Presupuesto? Extraer(string mensaje)
    {
        var texto = Normalizar(mensaje);
        const string numero = @"\d+(?:[.,]\d{1,2})?(?![\d.,])";
        var coincidencias = Regex.Matches(texto,
            @"(?:(?<divisa>us\$|usd|pen|s/)\s*(?<monto>" + numero + @")|(?<monto>" + numero + @")\s*(?<divisa>soles?|dolares?|pen|usd)\b)");
        Match? match = coincidencias.Count > 0 ? coincidencias[^1] : null;
        match ??= Regex.Match(texto, @"\b(?:presupuesto|maximo|hasta|solo tengo|puedo pagar|dispongo de)\b[^\d]{0,35}(?<monto>" + numero + @")");
        if (!match.Success || !decimal.TryParse(match.Groups["monto"].Value.Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var monto)) return null;
        var divisa = match.Groups["divisa"].Value;
        string? moneda = divisa.Length == 0 ? null : divisa is "usd" or "us$" or "dolar" or "dolares" ? "USD" : "PEN";
        return new(monto, moneda);
    }

    internal static bool MismaMoneda(EventoChatDTO evento, Presupuesto presupuesto) => presupuesto.Moneda == null || presupuesto.Moneda == evento.Moneda;
    internal static string Responder(EventoChatDTO evento, Presupuesto presupuesto)
    {
        var precio = Dinero(evento.Precio, evento.Moneda);
        if (!MismaMoneda(evento, presupuesto))
            return $"La entrada de «{evento.Nombre}» cuesta {precio}. Tu presupuesto es {Dinero(presupuesto.Monto, presupuesto.Moneda!)}; necesito un presupuesto en {evento.Moneda} para compararlos sin asumir un tipo de cambio.";
        var disponible = Dinero(presupuesto.Monto, evento.Moneda);
        var diferencia = presupuesto.Monto - evento.Precio;
        if (diferencia < 0)
            return $"No, tu presupuesto de {disponible} no alcanza para «{evento.Nombre}». La entrada cuesta {precio} y te falta {Dinero(-diferencia, evento.Moneda)}.";
        if (diferencia == 0)
            return $"Sí, tu presupuesto de {disponible} alcanza exactamente para «{evento.Nombre}». La entrada cuesta {precio}.";
        return $"Sí, puedes pagar la entrada de «{evento.Nombre}», que cuesta {precio}. Con tu presupuesto de {disponible} te sobran {Dinero(diferencia, evento.Moneda)}.";
    }
    private static string Dinero(decimal monto, string moneda) => $"{(moneda == "USD" ? "US$" : "S/")} {monto.ToString("0.00", CultureInfo.InvariantCulture)}";
    private static string Normalizar(string texto) => new string(texto.Normalize(NormalizationForm.FormD)
        .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray()).ToLowerInvariant();
}

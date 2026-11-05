using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using EventosPeruIA.CORE.Core.DTOs;

namespace EventosPeruIA.CORE.Core.Services;

internal static class CalculoEntradas
{
    internal sealed record Resultado(string Texto, bool Recomendar);
    internal static bool EsConsulta(string texto) => Regex.IsMatch(Normalizar(texto),
        @"\b(presupuesto|alcanza|alcanzan|pagar|sobr[ae]|falt[ae]|total|cuesta|cuestan|costo|precio|debemos tener|somos|seremos|por persona|cada uno|cada una)\b");

    internal static Resultado? Responder(EventoChatDTO evento, IReadOnlyList<TurnoChat> turnos)
    {
        var ultimo = Normalizar(turnos.Last(t => t.Role == "user").Content);
        if (!EsConsulta(ultimo)) return null;
        if (Regex.IsMatch(ultimo, @"\d[.,]\d{3,}\b"))
            return new("Escribe los importes sin separadores de miles y con hasta dos decimales, por ejemplo 1000.50 soles.", false);
        if (Regex.IsMatch(ultimo, @"-\s*\d+\s*(soles|dolares|pen|usd)|(?:presupuesto|tengo|tenemos)\s*(?:de|es)?\s*-\s*\d"))
            return new("Indica un presupuesto no negativo.", false);
        int cantidad = 1;
        bool cantidadExplicita = false, ambiguo = false, porPersona = false, alcanceExplicito = false;
        RespuestasPresupuesto.Presupuesto? presupuesto = null;
        foreach (var turno in turnos.Where(t => t.Role == "user"))
        {
            var texto = Normalizar(turno.Content);
            var cantidades = Regex.Matches(texto, @"\b(?:somos|seremos)\s+(?<n>-?\d+(?:[.,]\d+)?)\b|\b(?<n>-?\d+(?:[.,]\d+)?)\s+(?:personas|adultos|entradas|boletos|tickets)\b")
                .Select(m => m.Groups["n"].Value).Distinct().ToList();
            var adicional = Regex.Match(texto, @"\bcon\s+(?<n>\d+)\s+(?:personas?|amigos?|acompanantes?)\s+mas\b");
            if (adicional.Success) cantidades = [(int.Parse(adicional.Groups["n"].Value) + 1).ToString()];
            if (Regex.IsMatch(texto, @"\b(solo yo|yo solo|yo sola|una entrada|un boleto|un ticket)\b")) cantidades = ["1"];
            if (cantidades.Count > 0)
            {
                ambiguo = cantidades.Count != 1 || !int.TryParse(cantidades[0], out cantidad) || cantidad is < 1 or > 1000 || Regex.IsMatch(texto, @"-\d+\s*(personas|entradas|boletos|tickets)");
                cantidadExplicita = true;
            }
            else if (Regex.IsMatch(texto, @"\b(con alguien|con amigos|varias personas|en grupo|mis amigos)\b")) ambiguo = true;
            var nuevo = RespuestasPresupuesto.Ultimo([new("user", texto)]);
            if (nuevo != null)
            {
                presupuesto = nuevo;
                porPersona = Regex.IsMatch(texto, @"\b(por persona|cada 1|cada persona)\b");
                alcanceExplicito = porPersona || Regex.IsMatch(texto, @"\b(total|entre todos|para todos|para el grupo)\b");
            }
            else if (Regex.IsMatch(texto, @"\b(por persona|cada 1|cada persona)\b")) { porPersona = true; alcanceExplicito = true; }
            else if (Regex.IsMatch(texto, @"\b(presupuesto total|es total|entre todos|para el grupo)\b")) { porPersona = false; alcanceExplicito = true; }
        }
        if (ambiguo) return new("¿Cuántas entradas necesitan exactamente? Indica una cantidad entera entre 1 y 1000.", false);
        if (Regex.IsMatch(ultimo, @"\b(ninos|menores|descuento|promocion|comision|impuestos)\b"))
            return new("Solo dispongo del precio publicado por entrada. No tengo tarifas especiales, descuentos ni cargos adicionales confirmados. ¿Calculamos con ese precio?", false);
        var total = evento.Precio * cantidad;
        var comparar = Regex.IsMatch(ultimo, @"\b(presupuesto|alcanza|alcanzan|pagar|sobr[ae]|falt[ae]|tengo|tenemos|por persona|cada 1|es total|entre todos)\b");
        if (!cantidadExplicita && !comparar && !Regex.IsMatch(ultimo, @"\b(total|debemos tener)\b")) return null;
        if (cantidad == 1 && comparar && presupuesto != null)
            return new(RespuestasPresupuesto.Responder(evento, presupuesto), RespuestasPresupuesto.MismaMoneda(evento, presupuesto) && presupuesto.Monto >= total);
        var detalle = $"«{evento.Nombre}»: {cantidad} entrada(s) × {Dinero(evento.Precio, evento.Moneda)} = {Dinero(total, evento.Moneda)} en total.";
        if (cantidad > evento.CuposDisponibles)
            return new(detalle + $" Solo quedan {evento.CuposDisponibles} cupos; no hay disponibilidad para todo el grupo.", false);
        // Pedir precio/total no es lo mismo que preguntar si un presupuesto anterior alcanza.
        if (!comparar || presupuesto == null) return new(detalle, true);
        if (!RespuestasPresupuesto.MismaMoneda(evento, presupuesto))
            return new(detalle + $" El presupuesto está en {presupuesto.Moneda}; necesito el importe en {evento.Moneda} para compararlo sin inventar un tipo de cambio.", false);
        if (cantidadExplicita && cantidad > 1 && !alcanceExplicito)
            return new(detalle + $" Si {Dinero(presupuesto.Monto, evento.Moneda)} es el presupuesto total del grupo, " + Conclusion(presupuesto.Monto - total, evento.Moneda) + " ¿Ese presupuesto es total o por persona?", false);
        var disponible = presupuesto.Monto * (porPersona ? cantidad : 1);
        return new(detalle + $" Presupuesto {(porPersona ? "por persona" : "total")}: {Dinero(presupuesto.Monto, evento.Moneda)}. " + Conclusion(disponible - total, evento.Moneda), disponible >= total);
    }
    private static string Conclusion(decimal diferencia, string moneda) => diferencia < 0 ? $"No alcanza; faltan {Dinero(-diferencia, moneda)}." : diferencia == 0 ? "Alcanza exactamente." : $"Alcanza; sobran {Dinero(diferencia, moneda)}.";
    private static string Dinero(decimal monto, string moneda) => $"{(moneda == "PEN" ? "S/" : moneda == "USD" ? "US$" : moneda)} {monto.ToString("0.00", CultureInfo.InvariantCulture)}";
    private static string Normalizar(string texto)
    {
        var normal = new string(texto.Normalize(NormalizationForm.FormD).Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray()).ToLowerInvariant();
        var palabras = new Dictionary<string, int> { ["una"] = 1, ["un"] = 1, ["uno"] = 1, ["dos"] = 2, ["tres"] = 3, ["cuatro"] = 4, ["cinco"] = 5, ["seis"] = 6, ["siete"] = 7, ["ocho"] = 8, ["nueve"] = 9, ["diez"] = 10 };
        return Regex.Replace(normal, @"\b(una|un|uno|dos|tres|cuatro|cinco|seis|siete|ocho|nueve|diez)\b", m => palabras[m.Value].ToString());
    }
}

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace EventosPeruIA.CORE.Core.Services;

public static class PromptInjectionGuard
{
    private static readonly string[] Patterns =
    [
        @"\b(ignore|ignora|olvida|forget)\b.{0,60}\b(previous|prior|above|anteriores?|previas?|system|sistema|instrucciones?)\b",
        @"\b(reveal|show|print|muestra|revela|imprime)\b.{0,50}\b(system prompt|prompt del sistema|instrucciones internas|secretos? del sistema|claves? (de api|secretas)|api key|configuracion interna)\b",
        @"\b(override|bypass|jailbreak|ignora las reglas|cambiar instrucciones)\b",
        @"\b(you are now|ahora eres|actua como)\b.{0,30}\b(system|developer|admin|root|administrador)\b",
        @"\b(ejecuta|execute|run)\b.{0,40}\b(sql|query|comando|shell)\b",
        @"\b(exfiltrate|exfiltra|steal|roba)\b.{0,40}\b(data|datos|secrets|secretos|credentials|credenciales)\b"
    ];

    public static bool IsInjectionAttempt(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        var normalized = Normalize(text);
        return Patterns.Any(pattern => Regex.IsMatch(normalized, pattern, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100)));
    }

    public static string Normalize(string text)
    {
        var decomposed = text.Normalize(NormalizationForm.FormD);
        var withoutDiacritics = new string(decomposed
            .Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            .ToArray());
        return Regex.Replace(withoutDiacritics.ToLowerInvariant(), @"[^\p{L}\p{N}]+", " ").Trim();
    }
}

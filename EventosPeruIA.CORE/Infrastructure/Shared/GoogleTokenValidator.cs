using System.Text.Json;
using EventosPeruIA.CORE.Core.Interfaces;
using Microsoft.Extensions.Configuration;
namespace EventosPeruIA.CORE.Infrastructure.Shared;

/// <summary>Valida el ID token con el endpoint oficial tokeninfo de Google y comprueba audiencia y emisor.</summary>
public sealed class GoogleTokenValidator(HttpClient http, IConfiguration config) : IGoogleTokenValidator
{
    public async Task<GoogleIdentity?> ValidateAsync(string idToken, CancellationToken ct)
    {
        var clientId = config["Google:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId))
            throw new InvalidOperationException("Configura Google:ClientId (user-secrets o variable de entorno).");
        if (string.IsNullOrWhiteSpace(idToken) || idToken.Length > 4096) return null;

        using var response = await http.GetAsync("https://oauth2.googleapis.com/tokeninfo?id_token=" + Uri.EscapeDataString(idToken), ct);
        if (!response.IsSuccessStatusCode) return null;

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var root = doc.RootElement;
        string? Text(string name) => root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

        var issuer = Text("iss");
        if (Text("aud") != clientId || (issuer != "accounts.google.com" && issuer != "https://accounts.google.com")) return null;

        var verified = root.TryGetProperty("email_verified", out var ev) &&
            (ev.ValueKind == JsonValueKind.True || (ev.ValueKind == JsonValueKind.String && string.Equals(ev.GetString(), "true", StringComparison.OrdinalIgnoreCase)));
        var subject = Text("sub");
        var email = Text("email");
        if (!verified || string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(email)) return null;

        return new GoogleIdentity(subject, email.Trim().ToLowerInvariant(), Text("given_name") ?? "", Text("family_name") ?? "");
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EventosPeruIA.CORE.Core.DTOs;
using EventosPeruIA.CORE.Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace EventosPeruIA.CORE.Infrastructure.Shared;

public sealed class GeminiModeloIA(HttpClient http, IConfiguration config) : IModeloIA
{
    private string EmbeddingModel => config["AI:Gemini:EmbeddingModel"] ?? "gemini-embedding-001";
    // El identificador distingue proveedor, modelo y dimensión de los vectores almacenados.
    public string ModeloEmbeddings => $"gemini/{EmbeddingModel}/768";

    public async Task<float[]> Embed(string texto, CancellationToken ct)
    {
        var json = await Enviar(EmbeddingModel, "embedContent", new
        {
            model = $"models/{EmbeddingModel}",
            content = new { parts = new[] { new { text = texto } } },
            outputDimensionality = 768
        }, ct);
        try
        {
            var vector = json.GetProperty("embedding").GetProperty("values").Deserialize<float[]>()!;
            if (vector == null || vector.Length != 768 || vector.Any(v => !float.IsFinite(v)) || vector.All(v => v == 0))
                throw new JsonException();
            return vector;
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        { throw new ProyectoException(502, "Gemini devolvió un embedding inválido."); }
    }

    public async Task<string> Responder(IReadOnlyList<TurnoChat> mensajes, CancellationToken ct)
    {
        var json = await Enviar(config["AI:Gemini:ChatModel"] ?? "gemini-3.5-flash-lite", "generateContent", new
        {
            systemInstruction = new { parts = new[] { new { text = mensajes[0].Content } } },
            contents = mensajes.Skip(1).Select(m => new
            {
                role = m.Role == "assistant" ? "model" : "user",
                parts = new[] { new { text = m.Content } }
            }),
            generationConfig = new
            {
                temperature = 0,
                responseMimeType = "application/json",
                responseJsonSchema = EsquemaChat.Esquema(mensajes[0].Content.StartsWith("Extrae"))
            }
        }, ct);
        try
        {
            var candidate = json.GetProperty("candidates")[0];
            if (candidate.GetProperty("finishReason").GetString() != "STOP") throw new JsonException();
            var text = string.Concat(candidate.GetProperty("content").GetProperty("parts").EnumerateArray()
                .Where(p => !p.TryGetProperty("thought", out var thought) || !thought.GetBoolean())
                .Select(p => p.TryGetProperty("text", out var value) ? value.GetString() : ""));
            if (string.IsNullOrWhiteSpace(text)) throw new JsonException();
            return text;
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException or IndexOutOfRangeException)
        { throw new ProyectoException(502, "Gemini no pudo completar una respuesta válida. Intenta nuevamente."); }
    }

    private async Task<JsonElement> Enviar(string model, string method, object body, CancellationToken ct)
    {
        var key = config["AI:Gemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(key)) throw new ProyectoException(503, "Configura AI:Gemini:ApiKey en secretos del servidor.");
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post,
                $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(model)}:{method}");
            request.Headers.Add("x-goog-api-key", key);
            request.Content = JsonContent.Create(body);
            using var response = await http.SendAsync(request, ct);
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
                throw new ProyectoException(429, "Gemini alcanzó su cuota o límite de solicitudes. Revisa la cuota del proyecto en AI Studio.");
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                throw new ProyectoException(503, "Gemini rechazó la clave o los permisos del proyecto. Revisa la configuración del servidor.");
            if (!response.IsSuccessStatusCode)
                throw new ProyectoException(503, "Gemini no está disponible o el modelo configurado no admite la solicitud.");
            return await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        }
        catch (HttpRequestException) { throw new ProyectoException(503, "No se pudo conectar con Gemini."); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { throw new ProyectoException(504, "Gemini tardó demasiado en responder."); }
        catch (JsonException) { throw new ProyectoException(502, "Gemini devolvió datos inválidos."); }
    }
}


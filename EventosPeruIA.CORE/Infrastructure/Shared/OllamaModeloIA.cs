using System.Net.Http.Json;
using System.Text.Json;
using EventosPeruIA.CORE.Core.DTOs;
using EventosPeruIA.CORE.Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace EventosPeruIA.CORE.Infrastructure.Shared;

// Contrato HTTP oficial de Ollama: https://docs.ollama.com/api
public sealed class OllamaModeloIA(HttpClient http, IConfiguration config) : IModeloIA
{
    public string ModeloEmbeddings => config["AI:Ollama:EmbeddingModel"] ?? "nomic-embed-text";
    public async Task<float[]> Embed(string texto, CancellationToken ct)
    {
        var json = await Enviar("api/embed", new { model = ModeloEmbeddings, input = texto, truncate = false }, ct);
        try { return json.GetProperty("embeddings")[0].Deserialize<float[]>() ?? throw new JsonException(); }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or IndexOutOfRangeException or InvalidOperationException)
        { throw new ProyectoException(502, "Respuesta de embeddings inválida."); }
    }
    public async Task<string> Responder(IReadOnlyList<TurnoChat> mensajes, CancellationToken ct)
    {
        var json = await Enviar("api/chat", new
        {
            model = config["AI:Ollama:ChatModel"] ?? "llama3.2", stream = false, format = Esquema(mensajes[0].Content.StartsWith("Extrae")),
            messages = mensajes.Select(m => new { role = m.Role, content = m.Content }), options = new { temperature = 0 }
        }, ct);
        try { return json.GetProperty("message").GetProperty("content").GetString() ?? throw new JsonException(); }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        { throw new ProyectoException(502, "Respuesta conversacional inválida."); }
    }
    private static JsonElement Esquema(bool filtros) => JsonSerializer.Deserialize<JsonElement>(filtros ? """
        {"type":"object","properties":{
          "ubicacion":{"type":["string","null"]},
          "categoria":{"type":["string","null"]},
          "desde":{"anyOf":[{"type":"string","pattern":"^[0-9]{4}-[0-9]{2}-[0-9]{2}$"},{"type":"null"}]},
          "hasta":{"anyOf":[{"type":"string","pattern":"^[0-9]{4}-[0-9]{2}-[0-9]{2}$"},{"type":"null"}]},
          "precioMaximo":{"type":["number","null"],"minimum":0}},
          "required":["ubicacion","categoria","desde","hasta","precioMaximo"],"additionalProperties":false}
        """ : """
        {"type":"object","properties":{
          "respuesta":{"type":"string"},
          "eventosIds":{"type":"array","items":{"type":"integer"}}},
          "required":["respuesta","eventosIds"],"additionalProperties":false}
        """);
    private async Task<JsonElement> Enviar(string ruta, object body, CancellationToken ct)
    {
        try
        {
            using var response = await http.PostAsJsonAsync(ruta, body, ct);
            if (!response.IsSuccessStatusCode) throw new ProyectoException(503, "El servicio de IA no está disponible. Verifica Ollama y sus modelos.");
            return await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        }
        catch (HttpRequestException) { throw new ProyectoException(503, "No se pudo conectar con Ollama."); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { throw new ProyectoException(504, "El servicio de IA tardó demasiado."); }
        catch (JsonException) { throw new ProyectoException(502, "El servicio de IA devolvió datos inválidos."); }
    }
}

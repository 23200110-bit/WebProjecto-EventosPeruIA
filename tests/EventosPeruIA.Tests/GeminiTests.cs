using System.Net;
using System.Text;
using System.Text.Json;
using EventosPeruIA.CORE.Core.DTOs;
using EventosPeruIA.CORE.Infrastructure.Shared;
using Microsoft.Extensions.Configuration;
using Xunit;

public sealed class GeminiTests
{
    private static IConfiguration Config(string key = "synthetic-key") => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["AI:Gemini:ApiKey"] = key }).Build();

    [Fact]
    public async Task Chat_usa_header_privado_historial_y_esquema()
    {
        var handler = new Handler(async request =>
        {
            Assert.DoesNotContain("synthetic-key", request.RequestUri!.ToString());
            Assert.Equal("synthetic-key", request.Headers.GetValues("x-goog-api-key").Single());
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
            Assert.Equal("model", body.RootElement.GetProperty("contents")[1].GetProperty("role").GetString());
            Assert.Equal("object", body.RootElement.GetProperty("generationConfig").GetProperty("responseJsonSchema").GetProperty("type").GetString());
            return Response("""{"candidates":[{"finishReason":"STOP","content":{"parts":[{"text":"{\"respuesta\":\"Disponible\",\"eventosIds\":[1]}"}]}}]}""");
        });
        var model = new GeminiModeloIA(new HttpClient(handler), Config());
        var result = await model.Responder([new("system", "Responde"), new("user", "Hola"), new("assistant", "Hola"), new("user", "Precio")], default);
        Assert.Contains("Disponible", result);
    }

    [Theory]
    [InlineData(429, 429)]
    [InlineData(403, 503)]
    [InlineData(500, 503)]
    public async Task Errores_no_exponen_respuestas_del_proveedor(int upstream, int expected)
    {
        var handler = new Handler(_ => Task.FromResult(new HttpResponseMessage((HttpStatusCode)upstream)
            { Content = new StringContent("synthetic-key detalle privado") }));
        var model = new GeminiModeloIA(new HttpClient(handler), Config());
        var error = await Assert.ThrowsAsync<ProyectoException>(() => model.Embed("Prueba", default));
        Assert.Equal(expected, error.Status);
        Assert.DoesNotContain("synthetic-key", error.Message);
    }

    [Fact]
    public async Task Embedding_identifica_modelo_dimension_y_rechaza_vector_incompatible()
    {
        var handler = new Handler(async request =>
        {
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
            Assert.Equal(768, body.RootElement.GetProperty("outputDimensionality").GetInt32());
            return Response("""{"embedding":{"values":[1,2]}}""");
        });
        var model = new GeminiModeloIA(new HttpClient(handler), Config());
        Assert.Equal("gemini/gemini-embedding-001/768", model.ModeloEmbeddings);
        Assert.Equal(502, (await Assert.ThrowsAsync<ProyectoException>(() => model.Embed("Prueba", default))).Status);
    }

    [Fact]
    public async Task Clave_ausente_no_envia_solicitudes()
    {
        var model = new GeminiModeloIA(new HttpClient(new Handler(_ => throw new Exception("No debe llamar"))), Config(""));
        Assert.Equal(503, (await Assert.ThrowsAsync<ProyectoException>(() => model.Embed("Prueba", default))).Status);
    }

    [GeminiFact]
    public async Task Gemini_real_responde_y_genera_768_dimensiones()
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
        var model = new GeminiModeloIA(http, Config(Environment.GetEnvironmentVariable("EVENTOS_TEST_GEMINI_KEY")!));
        var text = await model.Responder([new("system", "Devuelve JSON {\"respuesta\":\"texto\",\"eventosIds\":[]}."), new("user", "Di hola sin recomendar eventos")], default);
        using var json = JsonDocument.Parse(text);
        Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("respuesta").GetString()));
        Assert.Equal(768, (await model.Embed("Evento de prueba en Lima", default)).Length);
    }

    private static HttpResponseMessage Response(string json) => new(HttpStatusCode.OK)
        { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => respond(request);
    }
    private sealed class GeminiFactAttribute : FactAttribute
    {
        public GeminiFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("EVENTOS_TEST_GEMINI_KEY")))
                Skip = "Requiere EVENTOS_TEST_GEMINI_KEY; usa cuota real de Gemini.";
        }
    }
}

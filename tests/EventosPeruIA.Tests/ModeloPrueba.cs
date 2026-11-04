using System.Text.Json;
using EventosPeruIA.CORE.Core.DTOs;
using EventosPeruIA.CORE.Core.Interfaces;
public sealed class ModeloPrueba : IModeloIA
{
    public string ModeloEmbeddings => "prueba";
    public List<IReadOnlyList<TurnoChat>> Llamadas { get; } = [];
    public bool Fallar { get; set; }
    public int EventoId { get; set; } = 1;
    public string Filtros { get; set; } = "{}";
    public Task<float[]> Embed(string texto, CancellationToken ct) => Task.FromResult(new float[] { 1, 0 });
    public Task<string> Responder(IReadOnlyList<TurnoChat> mensajes, CancellationToken ct)
    {
        if (Fallar) throw new ProyectoException(503, "Modelo no disponible.");
        Llamadas.Add(mensajes);
        return Task.FromResult(mensajes[0].Content.StartsWith("Extrae") ? Filtros : JsonSerializer.Serialize(new { respuesta = "Hay un concierto disponible en Lima.", eventosIds = new[] { EventoId } }));
    }
}

using EventosPeruIA.CORE.Core.DTOs;
using EventosPeruIA.CORE.Core.Entities;
using EventosPeruIA.CORE.Core.Interfaces;
using EventosPeruIA.CORE.Core.Services;
using Xunit;

public class ChatGruposTests
{
    [Theory]
    [InlineData("Somos 3 personas. ¿Cuánto debemos tener en total para las entradas?", "S/ 255.00")]
    [InlineData("Ahora tengo 100 soles y quiero ir con una persona más. ¿Me alcanza para ese evento y cuánto me sobra?", "S/ 70.00")]
    [InlineData("Somos dos y tenemos 100 soles por persona. ¿Alcanza?", "S/ 30.00")]
    public async Task Seguimiento_usa_datos_actuales_y_guarda_calculo_sin_llamar_modelo(string mensaje, string esperado)
    {
        var repository = new Repository();
        var service = new AiService(repository, new NoModel(), TimeProvider.System);
        var result = await service.Chat(26, new() { Mensaje = mensaje, ConversacionId = 25 }, default);
        Assert.Contains(esperado, result.Respuesta);
        Assert.Equal(result.Respuesta, repository.ConversacionActual.MensajesChatbot.Last().Contenido);
    }
    private sealed class NoModel : IModeloIA
    {
        public string ModeloEmbeddings => "unused";
        public Task<float[]> Embed(string texto, CancellationToken ct) => throw new Exception("No debe generar embeddings para un cálculo directo");
        public Task<string> Responder(IReadOnlyList<TurnoChat> mensajes, CancellationToken ct) => throw new Exception("No debe solicitar cálculo a IA");
    }
    private sealed class Repository : IAiRepository
    {
        public ConversacionesChatbot ConversacionActual { get; } = new() { ConversacionId = 25, UsuarioId = 26, Activa = true };
        public Task<ConversacionesChatbot?> Conversacion(int id, int usuarioId, CancellationToken ct) => Task.FromResult<ConversacionesChatbot?>(ConversacionActual);
        public Task<List<MensajesChatbot>> Historial(int id, CancellationToken ct) => Task.FromResult(new List<MensajesChatbot>
        {
            new() { MensajeId = 1, Emisor = "Usuario", Contenido = "Eventos en Lima" },
            new() { MensajeId = 2, Emisor = "Asistente", Contenido = "Rock en Lima", EventosReferenciados = [1] }
        });
        public Task<List<EventoChatDTO>> EventosDisponibles(DateTime ahora, CancellationToken ct) => Task.FromResult(new List<EventoChatDTO>
        { new(1, "Rock en Lima 2026", "Concierto", "Conciertos", DateTime.UtcNow.Date.AddDays(30), null, "Lima", 85, 499) });
        public Task GuardarConversacion(ConversacionesChatbot conversacion, CancellationToken ct) => Task.CompletedTask;
        public Task<string> TextoEmbedding(EventoChatDTO evento, CancellationToken ct) => throw new NotSupportedException();
        public Task<List<EventosEmbeddings>> Embeddings(int[] ids, string modelo, CancellationToken ct) => throw new NotSupportedException();
        public void AgregarEmbedding(EventosEmbeddings embedding) => throw new NotSupportedException();
    }
}

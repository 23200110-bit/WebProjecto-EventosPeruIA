using EventosPeruIA.CORE.Core.Entities;
using EventosPeruIA.CORE.Core.Interfaces;
using EventosPeruIA.CORE.Core.Services;
using Xunit;

public sealed class EmbeddingCompatibilityTests
{
    [Fact]
    public async Task Recomendaciones_no_comparan_modelos_diferentes_con_igual_dimension()
    {
        Eventos Event(int id, string model, List<float> vector) => new()
        {
            EventoId = id, Nombre = "Prueba", FechaInicio = DateTime.UtcNow.AddMonths(2),
            Categoria = new() { Nombre = "Conciertos" },
            Aforo = 10, EventosEmbeddings = new() { ModeloUsado = model, VectorEmbedding = vector, Vigente = true }
        };
        var user = new Usuarios
        {
            UsuarioEventoInteraccion = [new() { Evento = Event(1, "modelo-anterior", [1, 0]) }, new() { Evento = Event(2, "gemini", [0, 1]) }]
        };
        var service = new RecommendationService(new Repository(user, [Event(3, "modelo-anterior", [0, 1]), Event(4, "gemini", [0, 1])]));
        var result = await service.GetForUserAsync(1);
        Assert.Equal(0m, result.Single(e => e.EventId == 3).Score);
        Assert.True(result.Single(e => e.EventId == 4).Score > 0);
    }
    private sealed class Repository(Usuarios user, IReadOnlyCollection<Eventos> events) : IRecommendationRepository
    {
        public Task<Usuarios?> GetUserWithSignalsAsync(int userId, CancellationToken cancellationToken = default) => Task.FromResult<Usuarios?>(user);
        public Task<IReadOnlyCollection<Eventos>> GetAvailableEventsAsync(CancellationToken cancellationToken = default) => Task.FromResult(events);
    }
}

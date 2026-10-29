using EventosPeruIA.CORE.Core.DTOs;
using EventosPeruIA.CORE.Core.Entities;
using EventosPeruIA.CORE.Core.Interfaces;

namespace EventosPeruIA.CORE.Core.Services;

public sealed class RecommendationService(IRecommendationRepository repository) : IRecommendationService
{
    public async Task<IReadOnlyCollection<RecommendationResponse>> GetForUserAsync(
        int userId,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var user = await repository.GetUserWithSignalsAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("User was not found.");
        var events = await repository.GetAvailableEventsAsync(cancellationToken);
        var interests = user.UsuarioIntereses.Select(interest => interest.CategoriaId).ToHashSet();
        var interactedEvents = user.UsuarioEventoInteraccion
            .Where(interaction => interaction.Evento?.EventosEmbeddings?.Vigente == true)
            .Select(interaction => interaction.Evento!)
            .DistinctBy(evento => evento.EventoId)
            .ToArray();
        var userVector = AverageVector(interactedEvents
            .Select(evento => evento.EventosEmbeddings?.VectorEmbedding)
            .Where(vector => vector is not null)
            .Select(vector => vector!));
        var safeLimit = Math.Clamp(limit, 1, 50);

        return events
            .Select(evento => Score(evento, user, interests, userVector))
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Event.FechaInicio)
            .Take(safeLimit)
            .Select(item => Map(item.Event, item.Score, item.Reason))
            .ToArray();
    }

    private static (Eventos Event, decimal Score, string Reason) Score(
        Eventos evento,
        Usuarios user,
        HashSet<int> interests,
        float[]? userVector)
    {
        var score = 0m;
        var reasons = new List<string>();

        if (interests.Contains(evento.CategoriaId))
        {
            score += 0.45m;
            reasons.Add("matches your interests");
        }

        if (!string.IsNullOrWhiteSpace(user.CiudadUbicacion) &&
            string.Equals(user.CiudadUbicacion, evento.Ciudad, StringComparison.OrdinalIgnoreCase))
        {
            score += 0.20m;
            reasons.Add("is in your preferred location");
        }

        var semanticScore = CosineSimilarity(userVector, evento.EventosEmbeddings?.Vigente == true ? evento.EventosEmbeddings.VectorEmbedding : null);
        if (semanticScore > 0)
        {
            score += semanticScore * 0.25m;
            reasons.Add("is semantically similar to your activity");
        }

        if (evento.FechaInicio <= DateTime.UtcNow.AddDays(14))
        {
            score += 0.10m;
            reasons.Add("is happening soon");
        }

        if (reasons.Count == 0)
        {
            reasons.Add("is an upcoming event with availability");
        }

        return (evento, score, string.Join(", ", reasons));
    }

    private static RecommendationResponse Map(Eventos evento, decimal score, string reason) => new()
    {
        EventId = evento.EventoId,
        Name = evento.Nombre,
        Description = evento.Descripcion,
        ImageUrl = evento.ImagenUrl,
        CategoryId = evento.CategoriaId,
        Category = evento.Categoria.Nombre,
        VenueName = evento.NombreLugar,
        Address = evento.Direccion,
        City = evento.Ciudad,
        StartDate = evento.FechaInicio,
        EndDate = evento.FechaFin,
        Price = evento.Precio,
        Currency = evento.Moneda,
        AvailableSpots = evento.Aforo - evento.Entradas.Count(ticket => ticket.Estado != "Anulada"),
        Score = decimal.Round(score, 4),
        Reason = reason
    };

    private static float[]? AverageVector(IEnumerable<List<float>> vectors)
    {
        var values = vectors.ToArray();
        if (values.Length == 0)
        {
            return null;
        }

        var dimensions = values[0].Count;
        if (dimensions == 0 || values.Any(v => v.Count != dimensions || v.Any(x => !float.IsFinite(x))))
        {
            return null;
        }

        var average = new float[dimensions];
        foreach (var vector in values)
        {
            for (var index = 0; index < dimensions; index++)
            {
                average[index] += vector[index];
            }
        }

        for (var index = 0; index < dimensions; index++)
        {
            average[index] /= values.Length;
        }

        return average;
    }

    private static decimal CosineSimilarity(float[]? first, List<float>? second)
    {
        if (first is null || second is null || first.Length == 0 || second.Count == 0)
        {
            return 0;
        }

        if (first.Length != second.Count || first.Any(x => !float.IsFinite(x)) || second.Any(x => !float.IsFinite(x))) return 0;
        var dimensions = first.Length;
        double dot = 0;
        double firstMagnitude = 0;
        double secondMagnitude = 0;

        for (var index = 0; index < dimensions; index++)
        {
            dot += first[index] * second[index];
            firstMagnitude += first[index] * first[index];
            secondMagnitude += second[index] * second[index];
        }

        if (firstMagnitude == 0 || secondMagnitude == 0)
        {
            return 0;
        }

        return (decimal)Math.Max(0, dot / (Math.Sqrt(firstMagnitude) * Math.Sqrt(secondMagnitude)));
    }
}

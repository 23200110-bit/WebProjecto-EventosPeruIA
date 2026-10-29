using EventosPeruIA.CORE.Core.DTOs;
using EventosPeruIA.CORE.Core.Entities;
using EventosPeruIA.CORE.Core.Interfaces;

namespace EventosPeruIA.CORE.Core.Services;

public sealed class EventReviewService(IEventReviewRepository repository) : IEventReviewService
{
    public async Task<EventReviewResponse> CreateAsync(
        int userId,
        int eventId,
        CreateEventReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Rating is < 1 or > 5)
        {
            throw new ArgumentException("Rating must be between 1 and 5.");
        }

        if (request.Comment?.Length > 1000)
        {
            throw new ArgumentException("Comment cannot exceed 1000 characters.");
        }

        var ticket = await repository.GetUsedTicketAsync(request.TicketId, userId, cancellationToken)
            ?? throw new InvalidOperationException("Only the user who used a valid ticket can review the event.");

        if (ticket.EventoId != eventId)
        {
            throw new InvalidOperationException("The ticket does not belong to the selected event.");
        }

        var eventEnd = ticket.Evento.FechaFin ?? ticket.Evento.FechaInicio;
        if (ticket.Evento.Estado != "Finalizado" && eventEnd > DateTime.UtcNow)
        {
            throw new InvalidOperationException("The event must be finished before it can be reviewed.");
        }

        if (await repository.HasActiveReviewAsync(userId, eventId, cancellationToken))
        {
            throw new InvalidOperationException("The user already has an active review for this event.");
        }

        var review = new Resenas
        {
            EntradaId = request.TicketId,
            EventoId = eventId,
            UsuarioId = userId,
            Calificacion = request.Rating,
            Comentario = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim(),
            Estado = "Activa",
            FechaCreacion = DateTime.UtcNow
        };

        await repository.AddAsync(review, cancellationToken);
        return Map(review);
    }

    public async Task<EventReviewSummaryResponse> GetPublicSummaryAsync(
        int eventId,
        CancellationToken cancellationToken = default)
    {
        var reviews = await repository.GetPublicReviewsAsync(eventId, cancellationToken);
        var summary = await repository.GetSummaryAsync(eventId, cancellationToken);

        return new EventReviewSummaryResponse
        {
            EventId = eventId,
            AverageRating = summary?.PromedioCalificacion ?? 0,
            TotalReviews = summary?.TotalResenas ?? reviews.Count,
            Reviews = reviews
                .Where(review => review.ResenaId.HasValue)
                .Select(review => new EventReviewResponse
                {
                    ReviewId = review.ResenaId!.Value,
                    EventId = review.EventoId ?? eventId,
                    UserId = review.UsuarioId ?? 0,
                    Rating = review.Calificacion ?? 0,
                    Comment = review.Comentario,
                    Status = "Activa",
                    CreatedAt = review.FechaCreacion ?? DateTime.MinValue
                })
                .ToArray()
        };
    }

    private static EventReviewResponse Map(Resenas review) => new()
    {
        ReviewId = review.ResenaId,
        EventId = review.EventoId,
        UserId = review.UsuarioId,
        Rating = review.Calificacion,
        Comment = review.Comentario,
        Status = review.Estado,
        CreatedAt = review.FechaCreacion
    };
}

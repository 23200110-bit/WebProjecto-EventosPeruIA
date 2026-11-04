using EventosPeruIA.CORE.Core.DTOs;
using EventosPeruIA.CORE.Core.Entities;
using EventosPeruIA.CORE.Core.Interfaces;
using EventosPeruIA.CORE.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EventosPeruIA.CORE.Infrastructure.Repositories;

public sealed class AiRepository(PostgresContext db) : IAiRepository
{
    private readonly List<EventosEmbeddings> pendientes = [];
    public Task<List<EventoChatDTO>> EventosDisponibles(DateTime ahora, CancellationToken ct) =>
        db.Eventos.AsNoTracking().Where(e => e.Estado == "Publicado" && e.EstadoModeracion == "Aprobado"
            && e.Organizador.Estado == "Activo" && e.FechaInicio > ahora
            && e.Aforo > e.Entradas.Count(t => t.Estado == "Valida" || t.Estado == "Utilizada"))
        .Select(e => new EventoChatDTO(e.EventoId, e.Nombre, e.Descripcion, e.Categoria.Nombre, e.FechaInicio, e.FechaFin,
            e.Direccion + ", " + e.Ciudad, e.Precio, e.Aforo - e.Entradas.Count(t => t.Estado == "Valida" || t.Estado == "Utilizada"), e.Moneda)).ToListAsync(ct);
    public Task<string> TextoEmbedding(EventoChatDTO evento, CancellationToken ct) =>
        db.Database.SqlQuery<string>($"SELECT public.fn_texto_embedding({evento.EventoId}) AS \"Value\"").SingleAsync(ct);
    public Task<ConversacionesChatbot?> Conversacion(int id, int usuarioId, CancellationToken ct) =>
        db.ConversacionesChatbot.SingleOrDefaultAsync(c => c.ConversacionId == id && c.UsuarioId == usuarioId && c.Activa, ct);
    public Task<List<MensajesChatbot>> Historial(int id, CancellationToken ct) => db.MensajesChatbot.AsNoTracking()
        .Where(m => m.ConversacionId == id).OrderByDescending(m => m.MensajeId).Take(20).ToListAsync(ct);
    public Task<List<EventosEmbeddings>> Embeddings(int[] ids, string modelo, CancellationToken ct) =>
        db.EventosEmbeddings.AsNoTracking().Where(e => ids.Contains(e.EventoId) && e.ModeloUsado == modelo && e.Vigente).ToListAsync(ct);
    public void AgregarEmbedding(EventosEmbeddings embedding) => pendientes.Add(embedding);
    public async Task GuardarConversacion(ConversacionesChatbot conversacion, CancellationToken ct)
    {
        await using var tx = db.Database.CurrentTransaction == null ? await db.Database.BeginTransactionAsync(ct) : null;
        foreach (var embedding in pendientes)
        {
            var vector = embedding.VectorEmbedding.ToArray();
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT public.fn_registrar_embedding_evento({embedding.EventoId}, {vector}, CAST({embedding.ModeloUsado} AS varchar))", ct);
        }
        if (conversacion.ConversacionId == 0) db.ConversacionesChatbot.Add(conversacion);
        await db.SaveChangesAsync(ct);
        if (tx != null) await tx.CommitAsync(ct);
    }
}

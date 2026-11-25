using EventosPeruIA.CORE.Core.DTOs;
using EventosPeruIA.CORE.Core.Entities;
using EventosPeruIA.CORE.Core.Interfaces;
using EventosPeruIA.CORE.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

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

    public Task<List<PendingDocumentSectionDTO>> PendingDocumentSections(int count, CancellationToken ct) =>
        db.Database.SqlQuery<PendingDocumentSectionDTO>($"""
            SELECT id AS "Id", source_type AS "SourceType", source_id AS "SourceId",
                   title AS "Title", content AS "Content"
            FROM public.document_sections
            WHERE embedding IS NULL
            ORDER BY id
            LIMIT {count}
            """).ToListAsync(ct);

    public async Task SaveDocumentChunks(PendingDocumentSectionDTO section,
        IReadOnlyList<(string Content, float[] Embedding)> chunks, CancellationToken ct)
    {
        if (chunks.Count == 0) return;
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        for (var index = 0; index < chunks.Count; index++)
        {
            var chunk = chunks[index];
            var vector = VectorLiteral(chunk.Embedding);
            if (index == 0)
            {
                await db.Database.ExecuteSqlInterpolatedAsync($"""
                    UPDATE public.document_sections
                    SET content = {chunk.Content}, embedding = CAST({vector} AS vector)
                    WHERE id = {section.Id}
                    """, ct);
            }
            else
            {
                await db.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO public.document_sections
                        (source_type, source_id, chunk_index, title, content, embedding)
                    VALUES ({section.SourceType}, {section.SourceId}, {index}, {section.Title},
                            {chunk.Content}, CAST({vector} AS vector))
                    ON CONFLICT (source_type, source_id, chunk_index) DO UPDATE
                    SET title = EXCLUDED.title, content = EXCLUDED.content, embedding = EXCLUDED.embedding
                    """, ct);
            }
        }
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            DELETE FROM public.document_sections
            WHERE source_type = {section.SourceType} AND source_id = {section.SourceId}
              AND chunk_index >= {chunks.Count}
            """, ct);
        await tx.CommitAsync(ct);
    }

    public Task<List<RagDocumentMatchDTO>> MatchDocuments(float[] queryEmbedding, double threshold, int count, CancellationToken ct)
    {
        var vector = VectorLiteral(queryEmbedding);
        return db.Database.SqlQuery<RagDocumentMatchDTO>($"""
            SELECT id AS "Id", title AS "Title", content AS "Content", similarity AS "Similarity"
            FROM public.match_documents(CAST({vector} AS vector(768)), {threshold}, {count})
            """).ToListAsync(ct);
    }

    private static string VectorLiteral(float[] vector) =>
        "[" + string.Join(",", vector.Select(value => value.ToString("R", CultureInfo.InvariantCulture))) + "]";
}

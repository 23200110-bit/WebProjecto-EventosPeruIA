using System.Text.RegularExpressions;
using EventosPeruIA.CORE.Core.Interfaces;

namespace EventosPeruIA.CORE.Core.Services;

public sealed class EmbeddingService(IAiRepository repository, IModeloIA model) : IEmbeddingService
{
    private const int ChunkSize = 400;
    private const int Overlap = 50;

    public async Task<int> ProcessPendingAsync(CancellationToken ct)
    {
        var sections = await repository.PendingDocumentSections(5, ct);
        foreach (var section in sections)
        {
            var chunks = new List<(string Content, float[] Embedding)>();
            foreach (var text in Chunk(section.Content))
            {
                var embedding = await model.Embed(text, "RETRIEVAL_DOCUMENT", ct);
                chunks.Add((text, embedding));
            }
            await repository.SaveDocumentChunks(section, chunks, ct);
        }
        return sections.Count;
    }

    internal static IReadOnlyList<string> Chunk(string text)
    {
        var words = Regex.Split(text.Trim(), @"\s+").Where(word => word.Length > 0).ToArray();
        if (words.Length == 0) return [];
        var result = new List<string>();
        for (var start = 0; start < words.Length; start += ChunkSize - Overlap)
        {
            result.Add(string.Join(" ", words.Skip(start).Take(ChunkSize)));
            if (start + ChunkSize >= words.Length) break;
        }
        return result;
    }
}

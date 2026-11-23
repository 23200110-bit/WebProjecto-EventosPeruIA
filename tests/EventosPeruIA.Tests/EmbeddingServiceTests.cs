using EventosPeruIA.CORE.Core.Services;
using Xunit;

public sealed class EmbeddingServiceTests
{
    [Fact]
    public void Chunk_uses_about_400_words_with_50_word_overlap()
    {
        var words = Enumerable.Range(0, 800).Select(index => $"w{index}").ToArray();
        var chunks = EmbeddingService.Chunk(string.Join(' ', words));
        var split = chunks.Select(chunk => chunk.Split(' ')).ToArray();

        Assert.Equal(3, split.Length);
        Assert.All(split, chunk => Assert.InRange(chunk.Length, 1, 400));
        Assert.Equal(split[0][350], split[1][0]);
        Assert.Equal(split[1][350], split[2][0]);
    }
}
